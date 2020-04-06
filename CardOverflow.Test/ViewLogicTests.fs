module ViewLogicTests

open CardOverflow.Debug
open Xunit
open System

[<Fact>]
let ``TimeSpan to string looks pretty`` (): unit =
    TimeSpan.FromSeconds 10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("1 min", x)
    TimeSpan.FromMinutes 10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("10 min", x)
    TimeSpan.FromHours   10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("10 h", x)
    TimeSpan.FromDays    10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("10 d", x)
    TimeSpan.FromDays   100.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("3.3 mo", x)
    TimeSpan.FromDays  1000.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("2.7 yr", x)
    -TimeSpan.FromSeconds 10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("-1 min", x)
    -TimeSpan.FromMinutes 10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("-10 min", x)
    -TimeSpan.FromHours   10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("-10 h", x)
    -TimeSpan.FromDays    10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("-10 d", x)
    -TimeSpan.FromDays   100.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("-3.3 mo", x)
    -TimeSpan.FromDays  1000.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("-2.7 yr", x)
    
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
    
<head>
    <style>
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
