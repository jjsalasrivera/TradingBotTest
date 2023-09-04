using System;
using System.Collections.Generic;
using Binance.Net.Clients;
using Binance.Net.Objects;
using TradingBot.libCommon;
using Binance.Net.Enums;
using System.Threading.Tasks;
using Serilog;
using System.Linq;
using System.Text.Json;

namespace TradingBot.libAPIManager
{
	public class BinanceAPIClient : IClientManager
	{
		private static readonly ILogger _logger = Log.ForContext<BinanceAPIClient>();
		readonly BinanceClient client = null;
		readonly string _key;
		readonly string _password;

		public BinanceAPIClient(string key, string pass)
		{
			_key = key;
			_password = pass;

			try
			{
				client = new BinanceClient( new BinanceClientOptions()
				{
					ApiCredentials = new BinanceApiCredentials( _key, _password )
				} );
			}
			catch(Exception ex)
			{
				_logger.Error( ex.Message );
			}
			

		}

		public async Task<IEnumerable<Candle>> GetHistoricAsync( TimeIntervalE intervalE, DateTime from, DateTime to, string symbol )
		{
			List<Candle> res = new();
			bool wasError = false;
			bool noSignal = false;
			do
			{
				var klinesResult = await client.SpotApi.ExchangeData.GetKlinesAsync( symbol, GetInterval( intervalE ), from, to, 1000 );
				if( !klinesResult.Success )
				{
					_logger.Error( $"Hubo un error al solicitar historico: {klinesResult.Error.Code} - {klinesResult.Error.Message}" );
					wasError = true;
				}
				else
				{
					noSignal = !klinesResult.Data.Any();
					res.AddRange( klinesResult.Data.Select( k => new Candle( intervalE, k.OpenTime, k.CloseTime, k.HighPrice, k.LowPrice, k.OpenPrice, k.ClosePrice, k.Volume ) ) );
				}
				from = res.Last().CloseTime.AddSeconds( 1 ).AddMilliseconds( -res.Last().CloseTime.Millisecond );
			} while( !wasError && from <= to && !noSignal );

			return wasError ? null : res;
		}

		public async Task<string> GetSymbolInfoAsync(string Symbol)
		{
			string res = "";
			var symbolInfoResult = await client.SpotApi.ExchangeData.GetExchangeInfoAsync( Symbol );

			if( !symbolInfoResult.Success )
				_logger.Error( $"Hubo un error al solicitar historico: {symbolInfoResult.Error.Code} - {symbolInfoResult.Error.Message}" );
			else
				res = JsonSerializer.Serialize( symbolInfoResult.Data.Symbols.First() );

			return res;
		}

		private KlineInterval GetInterval( TimeIntervalE interval)
		{
			KlineInterval klineInterval = KlineInterval.FiveMinutes;

			switch(interval)
			{
				case TimeIntervalE.FIVEM:
					klineInterval = KlineInterval.FiveMinutes;
					break;

				case TimeIntervalE.FIVETEENM:
					klineInterval = KlineInterval.FifteenMinutes;
					break;

				case TimeIntervalE.ONEH:
					klineInterval = KlineInterval.OneHour;
					break;

				case TimeIntervalE.FOURH:
					klineInterval = KlineInterval.FourHour;
					break;
				case TimeIntervalE.ONED:
					klineInterval = KlineInterval.OneDay;
					break;

				default:
					klineInterval = KlineInterval.FiveMinutes;
					break;

			}

			return klineInterval;
		}
	}
}
