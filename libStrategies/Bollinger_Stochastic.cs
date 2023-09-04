using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skender.Stock.Indicators;
using TradingBot.libCommon;

namespace TradingBot.libStrategies
{
	public class Bollinger_Stochastic : IStrategy
	{
		private static readonly ILogger _logger = Log.ForContext<Bollinger_Stochastic>();

		List<Candle> _candles;
		int _bollingerLength = 20;
		int _stochasticLength = 14;
		int _maxElements = 450;
		IEnumerable<BollingerBandsResult> _bollinger;
		IEnumerable<StochResult> _stoch;
		Dictionary<DateTime, (BollingerBandsResult boll, StochResult Stoch)> _groupedIndicators = new();

		public Bollinger_Stochastic( int BollingerLength, int StochasticLength, IEnumerable<Candle> Candles )
		{
			_candles = Candles.ToList();
			_bollingerLength = BollingerLength;
			_stochasticLength = StochasticLength;

			_maxElements = Math.Max(_bollingerLength + 5, StochasticLength + 3);

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
			Order res = new( OrderTypeE.NONE, null );

			_calc();

			var lastCalc = _groupedIndicators.Last();
			var nextToLast = _groupedIndicators.Reverse().Skip( 1 ).First();
			var candle = _candles.Where( c => c.CloseTime == lastCalc.Key ).First();
			var nextToLastCandle = _candles[ _candles.Count - 2 ];

			if( position.Postion == PositionE.OUT )
			{
				if( lastCalc.Value.Stoch.K < 20 && lastCalc.Value.Stoch.D < 20
					&& lastCalc.Value.Stoch.K >= lastCalc.Value.Stoch.D
					&& ( candle.Close < ( decimal )lastCalc.Value.boll.LowerBand
						|| candle.Open < ( decimal )lastCalc.Value.boll.LowerBand ))
				{
					res = new Order( OrderTypeE.BUY,  candle.Min * 0.97m );
				}
			}
			else if( position.Postion == PositionE.IN )
			{
				if( candle.Close > position.Value
					&& lastCalc.Value.Stoch.K  > 80 && lastCalc.Value.Stoch.D > 80
					&& lastCalc.Value.Stoch.K < lastCalc.Value.Stoch.D)
				{
					res = new Order( OrderTypeE.SELL, null );
				}
			}

			return res;
		}

		void _calc()
		{
			var quotes = _candles.ToQuotes();

			_bollinger = quotes.GetBollingerBands( _bollingerLength );
			_stoch = quotes.GetStoch( _stochasticLength );

			var combinedList = _bollinger
				.Join( _stoch, boll => boll.Date, stoch => stoch.Date, ( boll, stoch ) => new { Boll = boll, Stoch = stoch } )
				.OrderBy( a => a.Boll.Date ).ToList();

			_groupedIndicators.Clear();
			_groupedIndicators = combinedList.ToDictionary(
				combined => combined.Boll.Date,
				combined => (combined.Boll, combined.Stoch)
			);
		}
	}
}
