///
/// https://www.youtube.com/watch?v=9KlGOGj-iUY
///

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skender.Stock.Indicators;
using TradingBot.libCommon;

namespace TradingBot.libStrategies
{
	public class RSI_ADX_EMA : IStrategy
	{
		private static readonly ILogger _logger = Log.ForContext<RSI_ADX_EMA>();
		List<Candle> _candles;
		int _maxElements = 200;
		readonly int _EMALength;
		readonly int _RSILength;
		readonly int _ADXLength;
		Dictionary<DateTime, (EmaResult Ema, RsiResult Rsi, AdxResult Adx)> _groupedIndicators = new();
		IEnumerable<EmaResult> _ema;
		IEnumerable<RsiResult> _rsi;
		IEnumerable<AdxResult> _adx;

		public RSI_ADX_EMA( int EMALength, int RSILength, int ADXLength, IEnumerable<Candle> Candles )
		{
			_candles = Candles.ToList();
			_EMALength = EMALength;
			_RSILength = RSILength;
			_ADXLength = ADXLength;


			_maxElements = Math.Max( Math.Max( _EMALength + 250, _RSILength * 10 ), 2 * _ADXLength + 250 );
			if( _candles.Count < _maxElements )
			{
				_logger.Error( $"El numero de parametros debe ser al menos: {Math.Min( Math.Min( _EMALength + 250, _RSILength * 10 ), 2 * _ADXLength + 250 )}" );
			}
			else
				_calc();
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

			_calc();

			var lastCalc = _groupedIndicators.Last();
			var nextToLast = _groupedIndicators.Reverse().Skip( 1 ).First();
			var candle = _candles.Where( c => c.CloseTime == lastCalc.Key ).First();

			if( position.Postion == PositionE.OUT)
			{
				if( (decimal)lastCalc.Value.Ema.Ema.Value <= candle.Close
					&& nextToLast.Value.Rsi.Rsi.Value < 20 && lastCalc.Value.Rsi.Rsi.Value >= 20
					&& lastCalc.Value.Adx.Adx.Value > 30)
				{
					res = new Order( OrderTypeE.BUY, candle.Min * 0.95m );
				}
			}
			else if( position.Postion == PositionE.IN)
			{
				if( candle.Close > position.Value )
				{
					if( nextToLast.Value.Rsi.Rsi.Value > 80 )
					{
						res = new Order( OrderTypeE.SELL, null );
					}
					else
					{
						res = new Order( OrderTypeE.STOPLOSS, candle.Min * 0.95m );
					}
				}
			}


			return res;
		}

		private void _calc()
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
			_rsi = quotes.GetRsi( _RSILength );
			_adx = quotes.GetAdx( _ADXLength );

			var combinedList = _ema
				.Join( _rsi, ema => ema.Date, rsi => rsi.Date, ( ema, rsi ) => new { Ema = ema, Rsi = rsi } )
				.Join( _adx, combined => combined.Ema.Date, adx => adx.Date, ( combined, Adx ) => new { combined.Ema, combined.Rsi, Adx } )
				.OrderBy( a => a.Ema.Date ).ToList();

			_groupedIndicators.Clear();
			_groupedIndicators = combinedList.ToDictionary(
				combined => combined.Ema.Date,
				combined => (combined.Ema, combined.Rsi, combined.Adx)
			);
		}
	}
}
