using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skender.Stock.Indicators;
using TradingBot.libCommon;
using TradingBot.libCommon.Indicators;


namespace TradingBot.libStrategies
{
	public class VolumeDeviation : IStrategy
	{
		private static readonly ILogger _logger = Log.ForContext<VolumeDeviation>();

		int _volumeLength;
		int _emaLongLength;
		int _emaShortLength;
		int _trendingEmaLength;
		List<Candle> _candles;
		int _maxElements = 200;
		IEnumerable<StandardDeviationResult> _volumeDeviations;
		IEnumerable<EmaResult> _emaLong;
		IEnumerable<EmaResult> _emaShort;
		IEnumerable<EmaResult> _trendingEma;
		IEnumerable<StandardDeviationResult> _candleDeviation;
		decimal lastStopLoss = decimal.MaxValue;

		public VolumeDeviation(int VolumeLength, int EmaLong, int EmaShort, int TrendingEmaLength, IEnumerable<Candle> Candles )
		{
			_volumeLength = VolumeLength;
			_emaLongLength = EmaLong;
			_emaShortLength = EmaShort;
			_trendingEmaLength = TrendingEmaLength;
			_candles = Candles.ToList();

			_maxElements = Math.Max(Math.Max( Math.Max( _volumeLength, _emaLongLength ), _emaShortLength ), TrendingEmaLength);

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
			Order res = new Order( OrderTypeE.NONE, null );

			_calc();

			var lastVolumeDeviation = _volumeDeviations.Last();
			var lastEmaLong = _emaLong.Last();
			var lastEmaShort = _emaShort.Last();
			var prevLastEmaShort = _emaShort.ElementAt( _emaShort.Count() - 2 );
			var lastTrendingEma = _trendingEma.Last();
			var lastCandleDeviation = _candleDeviation.Last();
			var candle = _candles.Last();

			var ascendingTrending = candle.Open > ( decimal )lastTrendingEma.Ema.Value;

			if( candle.CloseTime != lastVolumeDeviation.DateTime )
				throw new ArgumentException( $"Close Time {candle.CloseTime} != VolumeTime {lastVolumeDeviation.DateTime}" );

			if( position.Postion == PositionE.OUT )
			{
				if( candle.Volume > lastVolumeDeviation.ValueUp
					&& candle.IsBullish()
					&& lastEmaShort.Ema.Value >= lastEmaLong.Ema.Value )
				{
					if( ascendingTrending )
					{
						//_logger.Information( $"Volume: {candle.Volume} - Average: {lastVolumeDeviation.Average} - STDesv: {lastVolumeDeviation.ValueUp - lastVolumeDeviation.Average}" );
						res = new Order( OrderTypeE.BUY, ( decimal )lastTrendingEma.Ema.Value * 0.98m );
						lastStopLoss = ( decimal )lastTrendingEma.Ema.Value * 0.98m;
					}
					else if( !ascendingTrending && Math.Abs( candle.Close - candle.Open ) > lastCandleDeviation.ValueUp * 1.02m )
					{
						res = new Order( OrderTypeE.BUY, candle.Open * 0.98m );
						lastStopLoss = candle.Open * 0.98m;
					}
				}
			}
			else if( position.Postion == PositionE.IN )
			{
				if( candle.Close > position.Value
					&& ( (candle.Close > ( decimal )lastTrendingEma.Ema.Value && lastEmaShort.Ema.Value < lastEmaLong.Ema.Value)
					|| ( candle.Close <= ( decimal )lastTrendingEma.Ema.Value  && prevLastEmaShort.Ema > lastEmaShort.Ema ) ))
				{
					res = new Order( OrderTypeE.SELL, null );
				}
				else if( candle.Close > position.Value
					&& (( Math.Min(candle.Close, candle.Open) > ( decimal )lastTrendingEma.Ema.Value && ( decimal )lastTrendingEma.Ema.Value * 0.98m > lastStopLoss)
					|| ( Math.Min( candle.Close, candle.Open ) <= ( decimal )lastTrendingEma.Ema.Value && candle.Close * 0.98m > lastStopLoss ) ))
				{
					res = new Order( OrderTypeE.STOPLOSS, ( decimal )lastTrendingEma.Ema.Value * 0.98m );
					lastStopLoss = ( decimal )lastTrendingEma.Ema.Value * 0.98m;
				}
			}

			return res;
		}

		void _calc()
		{
			_volumeDeviations = _candles.GetVolumeStandardDeviation( _volumeLength );
			var quotes = _candles.ToQuotes();
			_emaLong = quotes.GetEma( _emaLongLength );
			_emaShort = quotes.GetEma( _emaShortLength );
			_trendingEma = quotes.GetEma( _trendingEmaLength );
			_candleDeviation = _candles.GetCandleDeviation( _volumeLength );
		}
	}
}
