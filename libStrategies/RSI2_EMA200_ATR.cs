///
///	https://www.youtube.com/watch?v=2EgzgQn-MbU
///
using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skender.Stock.Indicators;
using TradingBot.libCommon;

namespace TradingBot.libStrategies
{
	public class RSI2_EMA200_ATR : IStrategy
	{
		private static readonly ILogger _logger = Log.ForContext<RSI2_EMA200_ATR>();
		List<Candle> _candles;
		private int _EMALength;
		int _maxElements = 200;
		private int _RSILength;
		private int _ATRLength;
		private double _ATRMultiplier;
		private IEnumerable<EmaResult> _ema;
		private IEnumerable<RsiResult> _rsi;
		private IEnumerable<AtrStopResult> _atr;
		Dictionary<DateTime, (EmaResult Ema, RsiResult Rsi, AtrStopResult Atr)> _groupedIndicators = new();

		private readonly int UPPERBAND = 90;
		private readonly int LOWERBAND = 10;
		private readonly decimal TAKEPROFFITPERCENT = (1.5m + 100m) / 100m;



		public RSI2_EMA200_ATR( int EMALength, int RSILength, int ATRLength, double ATRMultiplier, IEnumerable<Candle> Candles )
		{
			_candles = Candles.ToList();

			_EMALength = EMALength;
			_RSILength = RSILength;
			_ATRLength = ATRLength;
			_ATRMultiplier = ATRMultiplier;

			_maxElements = Math.Max( Math.Max( _EMALength + 250, _RSILength * 10 ), _ATRLength + 250 );

			if( _candles.Count < _maxElements )
			{
				_logger.Error( $"El numero de parametros debe ser al menos: {Math.Min( Math.Min( _EMALength + 250, _RSILength * 10 ), _ATRLength + 250 )}" );
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
					&& nextToLast.Value.Rsi.Rsi.Value <= LOWERBAND
					&& lastCalc.Value.Rsi.Rsi.Value > LOWERBAND )
				{

					res = new Order( OrderTypeE.BUY, lastCalc.Value.Atr.SellStop.HasValue ? lastCalc.Value.Atr.SellStop : candle.Min * 0.95m );
				}
			}
			else if( position.Postion == PositionE.IN )
			{
				if( candle.Close > position.Value )
				{
					if( candle.Close >= position.Value * TAKEPROFFITPERCENT )
					{
						res = new Order( OrderTypeE.SELL, null );
					}
					else
					{
						res = new Order(OrderTypeE.STOPLOSS, lastCalc.Value.Atr.SellStop.HasValue ? lastCalc.Value.Atr.SellStop : candle.Min * 0.95m );
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
			_atr = quotes.GetAtrStop( _ATRLength, _ATRMultiplier );

			var combinedList = _ema
				.Join( _rsi, ema => ema.Date, rsi => rsi.Date, ( ema, rsi ) => new { Ema = ema, Rsi = rsi } )
				.Join( _atr, combined => combined.Ema.Date, atr => atr.Date, ( combined, Atr ) => new { combined.Ema, combined.Rsi, Atr } )
				.OrderBy( a => a.Ema.Date ).ToList();

			_groupedIndicators.Clear();
			_groupedIndicators = combinedList.ToDictionary(
				combined => combined.Ema.Date,
				combined => (combined.Ema, combined.Rsi, combined.Atr)
			);
		}
	}
}
