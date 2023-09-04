using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skender.Stock.Indicators;
using TradingBot.libCommon;
using TradingBot.libCommon.Indicators;

namespace TradingBot.libStrategies
{
	public class SSLplusEMAStrategy : IStrategy
	{
		private static readonly ILogger _logger = Log.ForContext<SSLplusEMAStrategy>();

		List<Candle> _candles;
		int _EMALength = 200;
		int _SSLLength = 10;
		int _maxElements = 450;
		IEnumerable<EmaResult> _ema;
		IEnumerable<SSLResult> _ssl;
		Dictionary<DateTime, (EmaResult Ema, SSLResult Ssl)> _groupedIndicators = new();


		public SSLplusEMAStrategy( int EMALength, int SSLLength, IEnumerable<Candle> Candles )
		{
			_candles = Candles.ToList();
			_EMALength = EMALength;
			_SSLLength = SSLLength;

			_maxElements = _EMALength + 250;

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
				if( /*( decimal )lastCalc.Value.Ema.Ema.Value < candle.Close
					&& */nextToLast.Value.Ssl.HighValue < nextToLast.Value.Ssl.LowValue
					&& lastCalc.Value.Ssl.HighValue >= lastCalc.Value.Ssl.LowValue)
				{
					res = new Order( OrderTypeE.BUY, lastCalc.Value.Ssl.LowValue * 0.95m );
				}
			}
			else if( position.Postion == PositionE.IN )
			{
				if( candle.Close > position.Value
					&& nextToLast.Value.Ssl.HighValue >= nextToLast.Value.Ssl.LowValue
					&& lastCalc.Value.Ssl.HighValue < lastCalc.Value.Ssl.LowValue )
				{
					res = new Order( OrderTypeE.SELL, null );	
				}
			}

			return res;
		}

		void _calc()
		{
			_ema = _candles.ToQuotes().GetEma( _EMALength );
			_ssl = _candles.GetSSL( _SSLLength );

			var combinedList = _ema
				.Join( _ssl, ema => ema.Date, ssl => ssl.DateTime, ( ema, ssl ) => new { Ema = ema, Ssl = ssl } )
				.OrderBy( a => a.Ema.Date ).ToList();

			_groupedIndicators.Clear();
			_groupedIndicators = combinedList.ToDictionary(
				combined => combined.Ema.Date,
				combined => (combined.Ema, combined.Ssl)
			);
		}
	}
}
