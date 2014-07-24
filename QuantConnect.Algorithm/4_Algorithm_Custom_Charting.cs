﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using QuantConnect.Securities;
using QuantConnect.Models;

namespace QuantConnect
{
    /// <summary>
    /// 4.0 DEMONSTRATION OF CUSTOM CHARTING FLEXIBILITY:
    /// 
    /// The entire charting system of quantconnect is adaptable. You can adjust it to draw whatever you'd like.
    /// 
    /// Charts can be stacked, or overlayed on each other.
    /// Series can be candles, lines or scatter plots.
    /// 
    /// Even the default behaviours of QuantConnect can be overridden
    /// 
    /// </summary>
    public class CustomChartingAlgorithm : QCAlgorithm
    {
        decimal lastPrice = 0;
        decimal fastMA = 0;
        decimal slowMA = 0;

        DateTime resample = new DateTime();
        TimeSpan resamplePeriod = new TimeSpan();

        DateTime startDate = new DateTime(2010, 3, 3);
        DateTime endDate = new DateTime(2014, 3, 3);

        /// <summary>
        /// Called at the start of your algorithm to setup your requirements:
        /// </summary>
        public override void Initialize()
        {
            //Set the date range you want to run your algorithm:
            SetStartDate(startDate);
            SetEndDate(endDate);

            //Set the starting cash for your strategy:
            SetCash(100000);

            //Add any stocks you'd like to analyse, and set the resolution:
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "SPY", resolution: Resolution.Minute);

            //Chart - Master Container for the Chart:
            Chart stockPlot = new Chart("Trade Plot", ChartType.Overlay);
            //On the Trade Plotter Chart we want 3 series: trades and price:
            ChartSeries buyOrders = new ChartSeries("Buy", SeriesType.Scatter);
            ChartSeries sellOrders = new ChartSeries("Sell", SeriesType.Scatter);
            ChartSeries assetPrice = new ChartSeries("Price", SeriesType.Line);
            stockPlot.AddSeries(buyOrders);
            stockPlot.AddSeries(sellOrders);
            stockPlot.AddSeries(assetPrice);
            AddChart(stockPlot);

            Chart avgCross = new Chart("Strategy Equity", ChartType.Stacked);
            ChartSeries fastMA = new ChartSeries("FastMA", SeriesType.Line);
            ChartSeries slowMA = new ChartSeries("SlowMA", SeriesType.Line);
            avgCross.AddSeries(fastMA);
            avgCross.AddSeries(slowMA);
            AddChart(avgCross);

            resamplePeriod = TimeSpan.FromMinutes((endDate - startDate).TotalMinutes / 2000);
        }


        /// <summary>
        /// OnEndOfDay Event Handler - At the end of each trading day we fire this code.
        /// To avoid flooding, we recommend running your plotting at the end of each day.
        /// </summary>
        public override void OnEndOfDay()
        {
            //Log the end of day prices:
            Plot("Trade Plot", "Price", lastPrice);
        }


        /// <summary>
        /// On receiving new tradebar data it will be passed into this function. The general pattern is:
        /// "public void OnData( CustomType name ) {...s"
        /// </summary>
        /// <param name="data">TradeBars data type synchronized and pushed into this function. The tradebars are grouped in a dictionary.</param>
        public void OnData(TradeBars data)
        {
            lastPrice = data["SPY"].Close;

            if (fastMA == 0) fastMA = lastPrice;
            if (slowMA == 0) slowMA = lastPrice;

            fastMA = (0.01m * lastPrice) + (0.99m * fastMA);
            slowMA = (0.001m * lastPrice) + (0.999m * slowMA);

            if (Time > resample)
            {
                resample = Time.Add(resamplePeriod);
                Plot("Strategy Equity", "FastMA", fastMA);
                Plot("Strategy Equity", "SlowMA", slowMA);
            }


            //On the 5th days when not invested buy:
            if (!Portfolio.Invested && Time.Day % 13 == 0)
            {
                Order("SPY", (int)(Portfolio.Cash / data["SPY"].Close));
                Plot("Trade Plot", "Buy", lastPrice);
            }
            else if (Time.Day % 21 == 0 && Portfolio.Invested)
            {
                Plot("Trade Plot", "Sell", lastPrice);
                Liquidate();
            }
        }
    }
}