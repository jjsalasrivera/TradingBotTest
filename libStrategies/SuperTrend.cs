using System;
using System.Collections.Generic;
using System.Linq;
using Skender.Stock.Indicators;
using TradingBot.libCommon;

namespace TradingBot.libStrategies
{
	public class SuperTrend : IStrategy
	{
		List<Candle> _candles;
		IEnumerable<SuperTrendResult> _superTrend;
		int _maxElements = 50;

		public SuperTrend( IEnumerable<Candle> Candles )
		{
			_candles = Candles.ToList();

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
			Order res = new( OrderTypeE.NONE, null );

			_calc();

			var lastSuperTrend = _superTrend.Last();
			var beforeLastSuperTrend = _superTrend.ElementAt( _superTrend.Count() - 2 );
			var candle = _candles.Last();

			decimal minVal = Math.Min( candle.Close, candle.Open );

			if( position.Postion == PositionE.OUT )
			{
				if(beforeLastSuperTrend.UpperBand.HasValue && lastSuperTrend.LowerBand.HasValue )
				{
					res = new Order( OrderTypeE.BUY, lastSuperTrend.LowerBand * 0.92m);
				}
			}
			else if( position.Postion == PositionE.IN )
			{
				if(candle.Close > position.Value)
				{
					if( lastSuperTrend.UpperBand.HasValue )
					{
						res = new Order( OrderTypeE.SELL, null );
					}
					else if( lastSuperTrend.LowerBand * 0.90m > position.StopLoss.Value )
					{
						res = new Order( OrderTypeE.STOPLOSS, lastSuperTrend.LowerBand * 0.92m );
					}
				}
			}

			return res;
		}

		void _calc()
		{
			_superTrend = _candles.ToQuotes().GetSuperTrend(20);
		}
	}
}
