module ViewLogicTests

open CardOverflow.Debug
open CardOverflow.Test
open Xunit
open System
open NodaTime

[<Fact>]
let ``TimeSpan to string looks pretty`` (): unit =
    Duration.FromSeconds 10.123456789 |> ViewLogic.toString |> Assert.equal "1 min"
    Duration.FromMinutes 10.123456789 |> ViewLogic.toString |> Assert.equal "10 min"
    Duration.FromHours   10.123456789 |> ViewLogic.toString |> Assert.equal "10 h"
    Duration.FromDays    10.123456789 |> ViewLogic.toString |> Assert.equal "10 d"
    Duration.FromDays   100.123456789 |> ViewLogic.toString |> Assert.equal "3.3 mo"
    Duration.FromDays  1000.123456789 |> ViewLogic.toString |> Assert.equal "2.7 yr"
    -Duration.FromSeconds 10.123456789 |> ViewLogic.toString |> Assert.equal "-1 min"
    -Duration.FromMinutes 10.123456789 |> ViewLogic.toString |> Assert.equal "-10 min"
    -Duration.FromHours   10.123456789 |> ViewLogic.toString |> Assert.equal "-10 h"
    -Duration.FromDays    10.123456789 |> ViewLogic.toString |> Assert.equal "-10 d"
    -Duration.FromDays   100.123456789 |> ViewLogic.toString |> Assert.equal "-3.3 mo"
    -Duration.FromDays  1000.123456789 |> ViewLogic.toString |> Assert.equal "-2.7 yr"
    
let dateTime year month day hour minute second =
    Instant.FromUtc(year, month, day, hour, minute, second)
let dateYear year month day =
    Instant.FromUtc(year, month, day, 0, 0)

[<Fact>]
let ``timestampToPretty looks pretty`` (): unit =
    ViewLogic.timestampToPretty (dateYear 2000  1  1) (dateYear 2000 1 2      ) |> Assert.equal "1 d ago"
    ViewLogic.timestampToPretty (dateYear 2000  1  1) (dateTime 2000 1 2 1 1 1) |> Assert.equal "1 d ago"
    ViewLogic.timestampToPretty (dateYear 2000  1  1) (dateYear 2000 2 2      ) |> Assert.equal "on Jan 1 '00"
    ViewLogic.timestampToPretty (dateYear 2000  1  1) (dateYear 2001 2 2      ) |> Assert.equal "on Jan 1 '00"
    ViewLogic.timestampToPretty (dateYear 2001 12 31) (dateYear 2025 1 1      ) |> Assert.equal "on Dec 31 '01"
    ViewLogic.timestampToPretty (dateYear 2020 12 31) (dateYear 2025 1 1      ) |> Assert.equal "on Dec 31 '20"

[<Fact>]
let ``insertDiffColors`` (): unit =
    let actual = ViewLogic.insertDiffColors """<!DOCTYPE html>
            
<head>
    <style>
        .cloze-brackets-front {
            font-size: 150%;
            font-family: monospace;
            font-weight: bolder;
            color: dodgerblue;
        }
                    
        .cloze-filler-front {
            font-size: 150%;
            font-family: monospace;
            font-weight: bolder;
            color: dodgerblue;
        }
                    
        .cloze-brackets-back {
            font-size: 150%;
            font-family: monospace;
            font-weight: bolder;
            color: red;
        }
    </style>
    <style>
        .card {
            font-family: arial;
            font-size: 20px;
            text-align: center;
            color: black;
            background-color: white;
        }
    </style>
</head>
            
<body>
    <p>a front</p>
    <hr id=answer>
    <p>a back</p>
    <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script>
</body>
            
</html>
"""

    Assert.Equal("""<!DOCTYPE html>
    
<head><style>
        ins {
        	background-color: #cfc;
        	text-decoration: none;
        }
        
        del {
        	color: #999;
        	background-color:#FEC8C8;
        }
    </style>
    <style>
        .cloze-brackets-front {
    font-size: 150%;
    font-family: monospace;
    font-weight: bolder;
    color: dodgerblue;
        }
            
        .cloze-filler-front {
    font-size: 150%;
    font-family: monospace;
    font-weight: bolder;
    color: dodgerblue;
        }
            
        .cloze-brackets-back {
    font-size: 150%;
    font-family: monospace;
    font-weight: bolder;
    color: red;
        }
    </style>
    <style>
        .card {
    font-family: arial;
    font-size: 20px;
    text-align: center;
    color: black;
    background-color: white;
        }
    </style>
</head>
    
<body>
    <p>a front</p>
    <hr id=answer>
    <p>a back</p>
    <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script>
</body>
    
</html>
""", actual, ignoreWhiteSpaceDifferences = true, ignoreLineEndingDifferences = true)

[<Fact>]
let ``insertDiffColors works without <head>`` (): unit =
    let actual = ViewLogic.insertDiffColors """
    <p>a front</p>
    <hr id=answer>
    <p>a back</p>
"""

    Assert.Equal("""<style>
        ins {
        	background-color: #cfc;
        	text-decoration: none;
        }
        
        del {
        	color: #999;
        	background-color:#FEC8C8;
        }
    </style>
    <p>a front</p>
    <hr id=answer>
    <p>a back</p>
""", actual, ignoreWhiteSpaceDifferences = true, ignoreLineEndingDifferences = true)
