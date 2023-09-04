///
/// https://www.youtube.com/watch?v=vCOfaRM1sTM
///

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skender.Stock.Indicators;
using TradingBot.libCommon;

namespace TradingBot.libStrategies
{
	public class RSI_Stochastic_Hull : IStrategy
	{
		private static readonly ILogger _logger = Log.ForContext<RSI_Stochastic_Hull>();

		readonly int _EMALength;
		readonly int _rsiPeriods;
		readonly int _stochPeriods;
		readonly int _signalPeriods;
		readonly int _HullLength;
		readonly int _maxElements = 200;
		List<Candle> _candles;
		IEnumerable<EmaResult> _ema;
		IEnumerable<StochRsiResult> _stochRsi;
		IEnumerable<HmaResult> _hull;
		Dictionary<DateTime, (EmaResult Ema, StochRsiResult StochRsi, HmaResult Hull)> _groupedIndicators = new();

		public RSI_Stochastic_Hull( int EMALength, int rsiPeriods, int stochPeriods, int signalPeriods, int HullLength, IEnumerable<Candle> Candles )
		{
			_EMALength = EMALength;
			_rsiPeriods = rsiPeriods;
			_stochPeriods = stochPeriods;
			_signalPeriods = signalPeriods;
			_HullLength = HullLength;

			_maxElements = Math.Max( Math.Max( EMALength, _rsiPeriods + _stochPeriods + _signalPeriods ), rsiPeriods + 100 );

			_candles = Candles.ToList();

			if( _candles.Count() < _EMALength || _candles.Count() < _HullLength )
				_logger.Error( "El historico es menor que el numero minimo para hacer calculos" );
			else if( _candles.Count() < _rsiPeriods + _stochPeriods + _signalPeriods || _candles.Count() < _rsiPeriods + 100 )
				_logger.Error( "El historico es menor que el numero minimo para hacer calculos" );
			else
				calc();
		}

		public void AddCandleToHistory( Candle candle )
		{
			_candles.Add( candle );

			if( _candles.Count > _maxElements )
				_candles.RemoveRange( 0, _candles.Count - _maxElements );
		}

		public Order CheckForOperation( Position position )
		{
			Order res = new Order( OrderTypeE.NONE, null );

			calc();

			if( _groupedIndicators.Count > 10 )
			{
				var lastCalc = _groupedIndicators.Last();
				var nextToLast = _groupedIndicators.Reverse().Skip( 1 ).First();
				var candle = _candles.Where( c => c.CloseTime == lastCalc.Key ).First();

				if( position.Postion == PositionE.OUT )
				{
					if( candle.Close > ( decimal )lastCalc.Value.Ema.Ema.Value &&
						lastCalc.Value.Hull.Hma.Value >= nextToLast.Value.Hull.Hma.Value &&
						_checkOverSell( nextToLast.Value.StochRsi ) )
					{
						res = new Order( OrderTypeE.BUY, candle.Min * 0.95m );
					}
				}
				else if( position.Postion == PositionE.IN )
				{
					if( candle.Close > position.Value )
					{
						if( lastCalc.Value.Hull.Hma.Value < nextToLast.Value.Hull.Hma.Value )
						{
							res = new Order( OrderTypeE.SELL, null );
						}
						else
						{
							res = new Order( OrderTypeE.STOPLOSS, candle.Min * 0.95m );
						}
					}
				}
			}
			return res;
		}

		private void calc()
		{
			var quotes = _candles.Select( q => new Quote()
			{
				Close = q.Close,
				High = q.Max,
				Low = q.Min,
				Open = q.Open,
				Date = q.CloseTime,
				Volume = q.Volume
			} );

			_ema = quotes.GetEma( _EMALength );
			_stochRsi = quotes.GetStochRsi( _rsiPeriods, _stochPeriods, _signalPeriods );
			_hull = quotes.GetHma( _HullLength );

			var combinedList = _ema
				.Join( _stochRsi, ema => ema.Date, stochRsi => stochRsi.Date, ( ema, stochRsi ) => new { Ema = ema, StochRsi = stochRsi } )
				.Join( _hull, combined => combined.Ema.Date, hull => hull.Date, ( combined, hull ) => new { Ema = combined.Ema, StochRsi = combined.StochRsi, Hull = hull } )
				.OrderBy( a => a.Ema.Date ).ToList();

			_groupedIndicators.Clear();
			_groupedIndicators = combinedList.ToDictionary(
				combined => combined.Ema.Date,
				combined => (combined.Ema, combined.StochRsi, combined.Hull)
			);
		}

		private bool _checkOverSell( StochRsiResult rsi ) => rsi.StochRsi < 20 && rsi.Signal < 20;
	}
}
