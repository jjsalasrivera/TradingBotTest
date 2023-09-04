using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skender.Stock.Indicators;
using TradingBot.libCommon;
using TradingBot.libCommon.Indicators;

namespace TradingBot.libStrategies
{
	public class SSL_DPO : IStrategy
	{
		private static readonly ILogger _logger = Log.ForContext<SSL_DPO>();

		int _SSLDepth;
		int _DPODepth;
		int _ATRDepth;
		double _ATRMultiplier;
		List<Candle> _candles;
		int _maxElements = 260;
		IEnumerable<AtrStopResult> _atrStop;
		IEnumerable<SSLResult> _ssl;
		IEnumerable<DPOResult> _dpo;
		IEnumerable<EmaResult> _ema;

		public SSL_DPO(int SSLDepth, int DPODepth, int ATRDepth, double ATRMultiplier, IEnumerable<Candle> Candles )
		{
			_candles = Candles.ToList();
			_maxElements = Math.Max( SSLDepth + 100, Math.Max( ATRDepth, _DPODepth ) );

			_SSLDepth = SSLDepth;
			_ATRDepth = ATRDepth;
			_DPODepth = DPODepth;
			_ATRMultiplier = ATRMultiplier;

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

			var lastDPO = _dpo.Last();
			var lastSSL = _ssl.Last();
			var lastATR = _atrStop.Last();
			var lastEMA = _ema.Last();
			var prevLastEMA = _ema.ElementAt(_ema.Count() -2);

			var candle = _candles.Last();

			var stopLoss = Math.Min( candle.Open, candle.Close ) * 0.93m;
			//if( lastATR.SellStop.HasValue )
				//stopLoss = ( decimal )lastATR.SellStop;

			if( position.Postion == PositionE.OUT )
			{
				if( (decimal) lastEMA.Ema < candle.Min 
				&& prevLastEMA.Ema <= lastEMA.Ema / 1.0048d
				&& lastSSL.HighValue > lastSSL.LowValue 
				&& lastDPO.Value > 0 )
				{
					res = new Order( OrderTypeE.BUY, stopLoss );
				}
			}
			else if( position.Postion == PositionE.IN )
			{
				if(candle.Close > position.Value )
				{
					if(lastSSL.HighValue <= lastSSL.LowValue)
					{
						res = new Order( OrderTypeE.SELL );
					}
					else if( stopLoss > position.StopLoss )
					{
						res = new Order( OrderTypeE.STOPLOSS, stopLoss );
					}
				}
			}

			return res;
		}

		void _calc()
		{
			var quotes = _candles.ToQuotes();
			_atrStop = quotes.GetAtrStop( _ATRDepth, _ATRMultiplier );
			_ssl = _candles.GetSSL( _SSLDepth );
			_dpo = _candles.GetDPO( _DPODepth );
			_ema = quotes.GetEma( 100 );
		}
	}
}
