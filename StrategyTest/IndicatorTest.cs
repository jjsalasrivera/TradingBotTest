using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Skender.Stock.Indicators;
using TradingBot.libAPIManager;
using TradingBot.libCommon;
using TradingBot.libCommon.Indicators;

namespace TradingBot.StrategyTest
{
	[TestClass]
	public class IndicatorTest
	{
		readonly string ConfigurationFile = "Configuration.json";
		Configuration config = null;
		private static readonly ILogger _logger = Log.ForContext<IndicatorTest>();


		[TestInitialize]
		public void InitialMethod()
		{
			string jsonString = File.ReadAllText( ConfigurationFile );
			config = JsonSerializer.Deserialize<Configuration>( jsonString )!;
			Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File( "InicatorTest.log" ).CreateLogger();
		}

		[TestMethod]
		public async Task SSLTest()
		{
			IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );
			var historic = await clientManager.GetHistoricAsync( TimeIntervalE.FIVEM, new DateTime( 2023, 6, 15 ), DateTime.Today, "BTCUSDT" );

			var miSSL = historic.GetSSL( 10 );

			foreach(var ssl in miSSL)
			{
				_logger.Information( $"Datetime: {ssl.DateTime} - High: {ssl.HighValue} - Low: {ssl.LowValue}" );

			}
		}

		[TestMethod]
		public async Task EMATest()
		{
			IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );
			var historic = await clientManager.GetHistoricAsync( TimeIntervalE.FIVEM, new DateTime( 2023, 6, 1 ), DateTime.Today, "BTCUSDT" );

			var miSMA = historic.Select( c => c.Close ).GetSMA( 20 );
			var suSMA = historic.ToQuotes().GetSma( 20 ).Select( s => s.Sma );

			_logger.Information( $"suSMA = {suSMA.Count()} - miSMA = {miSMA.Count()}" );

			for( int i = 0; i < suSMA.Count(); i++ )
				_logger.Information( $"{suSMA.ElementAt(i)} - {miSMA.ElementAt(i)}" );
		}

		[TestMethod]
		public async Task GannTest()
		{
			IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );
			var historic = await clientManager.GetHistoricAsync( TimeIntervalE.FIVEM, new DateTime( 2023, 6, 15 ), DateTime.Now, "BTCUSDT" );

			var miGann = historic.GetGannHighLow( 13, 21 );
			_logger.Information( $"miGann = {miGann.Count()}" );

			foreach(var g in miGann)
				_logger.Information( $"{g.DateTime.ToLocalTime()} - {g.Value} - {g.Color}" );
		}

		[TestMethod]
		public async Task StandardDeviationTest()
		{
			IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );
			var historic = await clientManager.GetHistoricAsync( TimeIntervalE.FIVEM, new DateTime( 2023, 6, 29 ), DateTime.UtcNow, "BTCUSDT" );

			var stDeviation = historic.GetVolumeStandardDeviation( 20 );
			foreach( var st in stDeviation )
				_logger.Information( $" | {st.DateTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")} | {st.ValueDown} | {st.Average} | {st.ValueUp} | {st.ValueUp - st.Average} | {historic.First( h => h.CloseTime == st.DateTime).Volume}" );
		}

		[TestMethod]
		public async Task DPOTest()
		{
			IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );
			var historic = await clientManager.GetHistoricAsync( TimeIntervalE.ONEH, new DateTime( 2023, 6, 1 ), DateTime.UtcNow, "BTCUSDT" );

			var dpos = historic.GetDPO( 20 );
			foreach( var dpo in dpos )
				_logger.Information( $" | {dpo.DateTime.ToLocalTime().ToString( "yyyy/MM/dd HH:mm:ss" )} | {dpo.Value} | {historic.First(c => c.CloseTime == dpo.DateTime).Close}" );
		}

	}
}
