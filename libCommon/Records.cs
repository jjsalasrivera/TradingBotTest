using System;

namespace TradingBot.libCommon
{
    public record Candle( TimeIntervalE TimeIntervalE, DateTime OpenTime, DateTime CloseTime,
        decimal Max, decimal Min, decimal Open, decimal Close, decimal Volume );


    public record Order( OrderTypeE OrderTypeE, decimal? StopLoss = null);

    public record Position( PositionE Postion, decimal Value, decimal? StopLoss );

    public record GannHighLowResult( decimal Value, ColorE Color, DateTime DateTime );

    public record SSLResult( decimal HighValue, decimal LowValue, DateTime DateTime );

    public record StandardDeviationResult( decimal Average, decimal ValueUp, decimal ValueDown, DateTime DateTime );

    public record DPOResult(decimal Value, DateTime DateTime);
}
