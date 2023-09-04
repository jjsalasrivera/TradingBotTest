using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skender.Stock.Indicators;
using TradingBot.libCommon;
using TradingBot.libCommon.Indicators;

namespace TradingBot.libStrategies
{
	public class Gann_ADX_EMA_ATR : IStrategy
	{
		private static readonly ILogger _logger = Log.ForContext<Gann_ADX_EMA_ATR>();
		List<Candle> _candles;
		int _maxElements = 200;
		readonly int _EMALength;
		readonly int _ADXLength;
		readonly int _ATRLength;
		readonly double _ATRMultiplier;
		readonly int _GannHPeriod;
		readonly int _GannLPeriod;

		readonly int ADXLIMIT = 20;

		Dictionary<DateTime, (EmaResult Ema, AdxResult Adx, GannHighLowResult Gann, AtrStopResult Atr)> _groupedIndicators = new();

		IEnumerable<EmaResult> _ema;
		IEnumerable<AdxResult> _adx;
		IEnumerable<GannHighLowResult> _gann;
		private IEnumerable<AtrStopResult> _atr;

		public Gann_ADX_EMA_ATR( int EMALength, int ADXLength, int ATRLength,
			double ATRMultiplier, int GannHPeriod, int GannLPeriod, IEnumerable<Candle> Candles )
		{
			_candles = Candles.ToList();
			_EMALength = EMALength;
			_ADXLength = ADXLength;
			_ATRLength = ATRLength;
			_ATRMultiplier = ATRMultiplier;
			_GannHPeriod = GannHPeriod;
			_GannLPeriod = GannLPeriod;

			_maxElements = Math.Max( Math.Max( _EMALength + 250, _ATRLength + 250 ), 2 * _ADXLength + 250 );
			_maxElements = Math.Max( _maxElements, Math.Max( GannHPeriod, GannLPeriod ) );

			if( _candles.Count < _maxElements )
			{
				_logger.Error( $"El numero de parametros debe ser al menos: {_maxElements}" );
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

			if( position.Postion == PositionE.OUT )
			{
				if( ( decimal )lastCalc.Value.Ema.Ema.Value <= candle.Close
					&& lastCalc.Value.Adx.Adx.Value > ADXLIMIT
					&& lastCalc.Value.Gann.Color == ColorE.GREEN
					&& nextToLast.Value.Gann.Color == ColorE.RED)
				{
					res = new Order( OrderTypeE.BUY, lastCalc.Value.Atr.SellStop.HasValue ? lastCalc.Value.Atr.SellStop : candle.Min * 0.95m );
				}
			}
			else if( position.Postion == PositionE.IN )
			{
				if( candle.Close > position.Value )
				{
					if( lastCalc.Value.Gann.Color == ColorE.RED )
					{
						res = new Order( OrderTypeE.SELL, null );
					}
					else
					{
						res = new Order( OrderTypeE.STOPLOSS, lastCalc.Value.Atr.SellStop.HasValue ? lastCalc.Value.Atr.SellStop : candle.Min * 0.95m );
					}
				}
			}


			return res;
		}

		private void _calc()
		{
			var quotes = _candles.ToQuotes();

			_ema = quotes.GetEma( _EMALength );
			_adx = quotes.GetAdx( _ADXLength );
			_atr = quotes.GetAtrStop( _ATRLength, _ATRMultiplier );
			_gann = _candles.GetGannHighLow( _GannHPeriod, _GannLPeriod );

			var combinedList = _ema
				.Join( _adx, ema => ema.Date, adx => adx.Date, ( ema, adx ) => new { Ema = ema, Adx = adx } )
				.Join( _atr, combined => combined.Ema.Date, atr => atr.Date, ( combined, Atr ) => new { combined.Ema, combined.Adx, Atr } )
				.Join( _gann, combined => combined.Ema.Date, gann => gann.DateTime, ( comined, Gann ) => new { comined.Ema, comined.Adx, comined.Atr, Gann } )
				.OrderBy( a => a.Ema.Date ).ToList();

			_groupedIndicators.Clear();
			_groupedIndicators = combinedList.ToDictionary(
				combined => combined.Ema.Date,
				combined => (combined.Ema, combined.Adx, combined.Gann, combined.Atr)
			);
		}
	}
}
