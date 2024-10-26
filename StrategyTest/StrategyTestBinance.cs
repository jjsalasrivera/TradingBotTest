using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TradingBot.libAPIManager;
using TradingBot.libCommon;
using TradingBot.libStrategies;
using System;
using System.Text.Json;
using Serilog;
using System.Threading.Tasks;
using System.Linq;
using CsvHelper.TypeConversion;

namespace StrategyTest
{
    record CsvRegister( string Simbol, DateTime date, OrderTypeE OrderType, decimal simbolValue, decimal invertedQuantity, decimal Cash );

    [TestClass]
    public class StrategyTestBinance
    {
        Dictionary<string, IEnumerable<Candle>> historics = new();
        private static readonly ILogger _logger = Log.ForContext<StrategyTestBinance>();

        readonly DateTime dateFrom = new( 2022, 10, 1 );
        readonly decimal InitialCash = 500;
		readonly string ConfigurationFile = "Configuration.json";
        Configuration config = null;
        bool csvWrited = false;
        List<string> Simbolos = new() { "BTCUSDT" };    //, "BNBUSDT", "ETHUSDT", "SANDUSDT" };

        List<TimeIntervalE> Intervals = new() { TimeIntervalE.FIVEM, TimeIntervalE.FIVETEENM, TimeIntervalE.ONEH, TimeIntervalE.FOURH, TimeIntervalE.ONED };

        [TestInitialize]
        public void InitialMethod()
		{
            string jsonString = File.ReadAllText( ConfigurationFile );
            config = JsonSerializer.Deserialize<Configuration>( jsonString )!;

            Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File( "StrategyTest.log" ).CreateLogger();
        }

		#region SSL_DPO

        [TestMethod]
        public async Task SSL_DPO()
		{
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals )
            {
                DateTime minDate = DateTime.Today.AddDays( -300 );
                if( interval == TimeIntervalE.ONEH || interval == TimeIntervalE.FOURH || interval == TimeIntervalE.ONED )
                    minDate = DateTime.Today.AddYears( -2 );

                foreach( var symbol in Simbolos )
                {
                    IEnumerable<Candle> historic;
                    if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, minDate, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    IStrategy strategy = new SSL_DPO( 20, 20, 14, 1.5, historic.Take( 260 ) );
                    (var inicio, var fin) = RunTest( strategy, historic.Skip( 260 ), $"SSL_DPO{interval}_{symbol}.csv", symbol );
                    if( fin != null && inicio != null )
                    {
                        var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                        var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                        _logger.Information( $"SSL_DPO{interval}_{symbol} = {percent:0.000} %/dias" );
                    }
                    else
                    {
                        _logger.Information( $"SSL_DPO{interval}_{symbol} = No data" );
                    }
                }
            }
        }

		#endregion

		#region Super Trend

		[TestMethod]
        public async Task SuperTrend()
        {
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals )
            {
                foreach( var symbol in Simbolos )
                {
                    IEnumerable<Candle> historic;
                    if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, dateFrom, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    IStrategy strategy = new SuperTrend(historic.Take( 50 ) );
                    (var inicio, var fin) = RunTest( strategy, historic.Skip( 50 ), $"SuperTrend{interval}_{symbol}.csv", symbol );
                    if( fin != null && inicio != null )
                    {
                        var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                        var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                        _logger.Information( $"SuperTrend{interval}_{symbol} = {percent:0.000} %/dias" );
                    }
                    else
                    {
                        _logger.Information( $"SuperTrend{interval}_{symbol} = No data" );
                    }
                }
            }
        }

        #endregion

        #region EMA Cross

        [TestMethod]
        public async Task EmaCross()
        {
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals )
            {
                foreach( var symbol in Simbolos )
                {
                    IEnumerable<Candle> historic;
                    if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, dateFrom, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    IStrategy strategy = new EMACross( 20, 9, 100, historic.Take( 350 ) );
                    (var inicio, var fin) = RunTest( strategy, historic.Skip( 350 ), $"Volume_Deviation{interval}_{symbol}.csv", symbol );
                    if( fin != null && inicio != null )
                    {
                        var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                        var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                        _logger.Information( $"EmaCross{interval}_{symbol} = {percent:0.000} %/dias" );
                    }
                    else
                    {
                        _logger.Information( $"EmaCross{interval}_{symbol} = No data" );
                    }
                }
            }
        }

        #endregion


        #region Volume Deviation

        [TestMethod]
        public async Task VolumeDeviation()
        {
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals )
            {
                foreach( var symbol in Simbolos )
                {
                    IEnumerable<Candle> historic;
                    if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, dateFrom, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    IStrategy strategy = new VolumeDeviation( 100, 20,10, 100, historic.Take( 100 ) );
                    (var inicio, var fin) = RunTest( strategy, historic.Skip( 100 ), $"Volume_Deviation{interval}_{symbol}.csv", symbol );
                    if( fin != null && inicio != null )
                    {
                        var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                        var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                        _logger.Information( $"Volume_Deviation{interval}_{symbol} = {percent:0.000} %/dias" );
                    }
                    else
                    {
                        _logger.Information( $"Volume_Deviation{interval}_{symbol} = No data" );
                    }
                }
            }
        }

        #endregion

        #region Bollinger Bands + Stochastic

        [TestMethod]
        public async Task Bollinger_Stoch()
		{
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals )
            {
                foreach( var symbol in Simbolos )
                {
                    IEnumerable<Candle> historic;
                    if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, dateFrom, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    IStrategy strategy = new Bollinger_Stochastic( 20, 14, historic.Take( 25 ) );
                    (var inicio, var fin) = RunTest( strategy, historic.Skip( 25 ), $"Bollinger_Stoch_{interval}_{symbol}.csv", symbol );
                    if( fin != null && inicio != null )
                    {
                        var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                        var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                        _logger.Information( $"Bollinger_Stoch_{interval}_{symbol} = {percent:0.000} %/dias" );
                    }
                    else
                    {
                        _logger.Information( $"Bollinger_Stoch_{interval}_{symbol} = No data" );
                    }
                }
            }
        }

		#endregion

		#region SSL + EMA

		[TestMethod]
        public async Task SSL_EMA_Async()
        {
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals )
            {
                foreach( var symbol in Simbolos )
                {
					IEnumerable<Candle> historic;
					if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, dateFrom, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    IStrategy strategy = new SSLplusEMAStrategy( 200, 10, historic.Take( 450 ) );
                    (var inicio, var fin) = RunTest( strategy, historic.Skip( 450 ), $"SSL_EMA_{interval}_{symbol}.csv", symbol );
                    if( fin != null && inicio != null )
                    {
                        var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                        var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                        _logger.Information( $"SSL_EMA_{interval}_{symbol} = {percent:0.000} %/dias" );
                    }
                    else
                    {
                        _logger.Information( $"SSL_EMA_{interval}_{symbol} = No data" );
                    }
                }
            }
        }

        #endregion

        #region RSI(2) + EMA(200) + ATR(14, 1.5)

        [TestMethod]
        public async Task RSI2_EMA200_ATRAsync()
		{
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals )
            {
                foreach( var symbol in Simbolos )
                {
                    IEnumerable<Candle> historic = null;
                    if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, dateFrom, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    IStrategy strategy = new RSI2_EMA200_ATR( 200, 2, 14, 1.5, historic.Take( 450 ) );
					(var inicio, var fin) = RunTest( strategy, historic.Skip( 450 ), $"RSI2_EMA_ATR_{interval}_{symbol}.csv", symbol );
                    if( fin != null && inicio != null )
					{
                        var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                        var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                        _logger.Information( $"RSI2_EMA_ATR_{interval}_{symbol} = {percent:0.000} %/dias" );
                    }
                    else
					{
                        _logger.Information( $"RSI2_EMA_ATR_{interval}_{symbol} = No data" );
                    }
                }
            }
        }

		#endregion

		#region RSI_ADX_EMA

		[TestMethod]
        public async Task RSIADXEMAAsync()
		{
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals)
			{
                foreach( var symbol in Simbolos )
                {
                    IEnumerable<Candle> historic = null;
                    if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, dateFrom, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    IStrategy strategy = new RSI_ADX_EMA( 50, 3, 5, historic.Take( 300 ) );
                    (var inicio, var fin) = RunTest( strategy, historic.Skip( 300 ), $"RSI_ADX_EMA_{interval}_{symbol}.csv", symbol );
                    if( fin != null && inicio != null )
                    {
                        var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                        var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                        _logger.Information( $"RSI_ADX_EMA_{interval}_{symbol} = {percent:0.000} %/dias" );
                    }
                    else
					{
                        _logger.Information( $"RSI_ADX_EMA_{interval}_{symbol} = No data" );
                    }
                }
            }
        }

        #endregion

        #region RSI Stochastic + Hull + EMA

        [TestMethod]
        public async Task RSIStochHullsync()
        {
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals )
            {
                foreach( var symbol in Simbolos )
                {
                    IEnumerable<Candle> historic = null;
                    if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, dateFrom, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    IStrategy strategy = new RSI_Stochastic_Hull( 200, 14, 14, 3, 12, historic.Take( 200 ) );
                    (var inicio, var fin) = RunTest( strategy, historic.Skip( 200 ), $"RSIStochHull_{interval}_{symbol}.csv", symbol );
                    if( fin != null && inicio != null )
                    {
                        var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                        var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                        _logger.Information( $"RSIStochHull_{interval}_{symbol} = {percent:0.000} %/dias" );
                    }
                    else
					{
                        _logger.Information( $"RSIStochHull_{interval}_{symbol} = No data" );
                    }
                }
            }
        }

        #endregion

        #region Gann_ADX_EMA_ATR

        [TestMethod]
        public async Task Gann_ADX_EMA_ATR_Async()
        {
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals )
            {
                foreach( var symbol in Simbolos )
                {
                    IEnumerable<Candle> historic = null;
                    if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, dateFrom, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    IStrategy strategy = new Gann_ADX_EMA_ATR( 200, 14, 14, 1.5, 13, 21, historic.Take( 450 ) );
                    (var inicio, var fin) = RunTest( strategy, historic.Skip( 450 ), $"Gann_ADX_EMA_ATR{interval}_{symbol}.csv", symbol );
                    if( fin != null && inicio != null )
                    {
                        var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                        var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                        _logger.Information( $"Gann_ADX_EMA_ATR{interval}_{symbol} = {percent:0.000} %/dias" );
                    }
					else
					{
                        _logger.Information( $"Gann_ADX_EMA_ATR{interval}_{symbol} = No data" );

                    }

                }
            }
        }

        #endregion

        #region Optimistic

        [TestMethod]
        public async Task Optimistic()
        {
            IClientManager clientManager = new BinanceAPIClient( config.BinanceClientConf.APIKey, config.BinanceClientConf.APIPass );

            foreach( TimeIntervalE interval in Intervals )
            {
                foreach( var symbol in Simbolos )
                {
                    IEnumerable<Candle> historic = null;
                    if( historics.ContainsKey( $"{symbol}_{interval}" ) )
                        historic = historics[ $"{symbol}_{interval}" ];
                    else
                    {
                        historic = await clientManager.GetHistoricAsync( interval, dateFrom, DateTime.Today, symbol );
                        historics[ $"{symbol}_{interval}" ] = historic;
                    }

                    List<decimal> percents = new() {0.01m, 0.015m, 0.020m, 0.025m, 0.030m, 0.035m, 0.040m, 0.045m, 0.050m, 0.055m, 0.060m };
                    for( int i = 0; i < 11; i++ )
                    {
                        decimal percentTosell = percents[i];
                        IStrategy strategy = new Optimistic(percentTosell);
                        (var inicio, var fin) = RunTest( strategy, historic, $"Optimistic_{interval}_{percentTosell}_{symbol}.csv", symbol );
                        
                        if( fin != null && inicio != null )
                        {
                            var dias = ( decimal )( fin.date - inicio.date ).TotalDays;
                            var percent = ( fin.Cash / InitialCash * 100 - 100 ) / dias;
                            _logger.Information( $"Optimistic_{interval}_{percentTosell}_{symbol} = {percent:0.000} %/dias" );
                        }
                        else
                        {
                            _logger.Information( $"OOptimistic_{interval}_{percentTosell}_{symbol} = No data" );
                        }
                    }
                
                }
            }
        }


        #endregion

        private (CsvRegister, CsvRegister) RunTest(IStrategy strategy, IEnumerable<Candle> candles, string fileName, string symbol)
		{
            bool operationOpened = false;
            decimal invertedQuantity = decimal.MinValue;
            decimal lastBuyPrice = decimal.MinValue;
            bool stopLossOpened = false;
            decimal stopLossValue = decimal.MinValue;
            decimal cash = InitialCash;

            CsvRegister reg_inicio = null; ;
            CsvRegister reg_fin = null;

            csvWrited = false;

            foreach( var candle in candles )
            {
                if( stopLossOpened && candle.Close <= stopLossValue)
				{
                    cash = Sell( invertedQuantity, candle.Close );
                    var reg = new CsvRegister( symbol, candle.CloseTime, OrderTypeE.STOPLOSS, candle.Close, invertedQuantity, cash );
                    ToCSV( fileName, reg );
                    stopLossOpened = false;
                    operationOpened = false;
                    reg_fin = reg;
                }

                strategy.AddCandleToHistory( candle );
                var op = strategy.CheckForOperation( new Position( ( operationOpened ? PositionE.IN : PositionE.OUT ), lastBuyPrice, stopLossOpened ? stopLossValue : null ) );

                if( op.OrderTypeE == OrderTypeE.BUY && !operationOpened )
                {
                    invertedQuantity = Buy( cash, candle.Close );
                    cash = 0;
                    lastBuyPrice = candle.Close;
                    operationOpened = true;

                    var reg = new CsvRegister( symbol, candle.CloseTime, OrderTypeE.BUY, candle.Close, invertedQuantity, cash );
                    if( reg_inicio == null )
                        reg_inicio = reg;

                    ToCSV( fileName, reg );

                    if( op.StopLoss != null )
                    {
                        stopLossValue = op.StopLoss.Value;
                        stopLossOpened = true;
                    }
                }
                else if( op.OrderTypeE == OrderTypeE.SELL && operationOpened )
                {
                    cash = Sell( invertedQuantity, candle.Close );
                    var reg = new CsvRegister( symbol, candle.CloseTime, OrderTypeE.SELL, candle.Close, invertedQuantity, cash );
                    ToCSV( fileName, reg );
                    operationOpened = false;
                    stopLossOpened = false;
                    reg_fin = reg;
                }
                else if( op.OrderTypeE == OrderTypeE.STOPLOSS && op.StopLoss != null)
                {
                    stopLossValue = op.StopLoss.Value;
                    stopLossOpened = true;
                }
            }

            return (reg_inicio, reg_fin);
		}

        private decimal Buy( decimal cash, decimal value ) => cash / value;

        private decimal Sell(decimal howMuch, decimal value) => howMuch * value;

        private void ToCSV(string fileName, CsvRegister register)
		{
			using var writer = new StreamWriter( fileName, true );
            using( var csv = new CsvWriter( writer, CultureInfo.InvariantCulture ) )
            {
                var options = new TypeConverterOptions { Formats = new[] { "yyyy-MM-dd HH:mm:ss" } };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>( options );

                if( !csvWrited)
				{
                    csv.WriteHeader<CsvRegister>();
                    csv.NextRecord();
                    csvWrited = true;
				}
                
                csv.WriteRecord( register );
                csv.NextRecord();
            }
        }
    }
}
