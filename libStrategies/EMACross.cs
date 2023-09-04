using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skender.Stock.Indicators;
using TradingBot.libCommon;

namespace TradingBot.libStrategies
{
	public class EMACross : IStrategy
	{
		private static readonly ILogger _logger = Log.ForContext<EMACross>();

		int _emaLongLength;
		int _emaShortLength;
		int _emaTrendingLength;
		List<Candle> _candles;
		int _maxElements = 200;
		IEnumerable<EmaResult> _emaLong;
		IEnumerable<EmaResult> _emaShort;
		IEnumerable<EmaResult> _emaTrending;

		public EMACross( int EmaLong, int EmaShort, int EmaTrending, IEnumerable<Candle> Candles )
		{
			_emaLongLength = EmaLong;
			_emaShortLength = EmaShort;
			_emaTrendingLength = EmaTrending;
			_candles = Candles.ToList();

			_maxElements = Math.Max(EmaTrending+250,Math.Max( _emaShortLength, _emaLongLength ));

			if( _candles.Count() < _maxElements )
			{
				_logger.Error( $"El numero de parametros debe ser al menos: {_maxElements}" );
			}

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

			var lastEmaLong = _emaLong.Last();
			var lastEmaShort = _emaShort.Last();
			var lastTrending = _emaTrending.Last();
			var prevLastTrending = _emaTrending.ElementAt(_emaTrending.Count() -2);

			var candle = _candles.Last();

			if(  position.Postion == PositionE.OUT  )
			{
				if( lastTrending.Ema > prevLastTrending.Ema &&  candle.IsBullish() && lastEmaShort.Ema.Value >= lastEmaLong.Ema.Value )
				{
					//_logger.Information( $"Volume: {candle.Volume} - Average: {lastVolumeDeviation.Average} - STDesv: {lastVolumeDeviation.ValueUp - lastVolumeDeviation.Average}" );
					res = new Order( OrderTypeE.BUY, null );
				}
			}
			else if( position.Postion == PositionE.IN )
			{
				if( lastEmaShort.Ema.Value < lastEmaLong.Ema.Value )
				{
					res = new Order( OrderTypeE.SELL, null );
				}
			}

			return res;
		}

		void _calc()
		{
			var quotes = _candles.ToQuotes();
			_emaLong = quotes.GetEma( _emaLongLength );
			_emaShort = quotes.GetEma( _emaShortLength );
			_emaTrending = quotes.GetEma( _emaTrendingLength );
		}
	}
}
