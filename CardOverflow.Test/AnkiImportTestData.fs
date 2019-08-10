module AnkiImportTestData

open CardOverflow.Api
open CardOverflow.Entity.Anki
open CardOverflow.Test
open CardOverflow.Pure
open CardOverflow.Debug
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open Helpers
open System.IO
open System.IO.Compression
open System.Linq
open Xunit
open CardOverflow.Sanitation
open System
open System.Runtime.CompilerServices

let allDefaultTemplatesAndImageAndMp3_apkg =
    {
        Cards = [
            CardEntity(
                Id = 1554689693560L,
                Nid = 1554689672054L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 1L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689744008L,
                Nid = 1554689697908L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 2L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689744009L,
                Nid = 1554689697908L,
                Did = 1L,
                Ord = 1L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 2L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689782082L,
                Nid = 1554689750007L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 3L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689782083L,
                Nid = 1554689750007L,
                Did = 1L,
                Ord = 1L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 3L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689799193L,
                Nid = 1554689787164L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 4L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689885405L,
                Nid = 1554689802142L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 5L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554690071566L,
                Nid = 1554689900910L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 6L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554690471678L,
                Nid = 1554690071622L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 7L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1560646410586L,
                Nid = 1560646400431L,
                Did = 1L,
                Ord = 0L,
                Mod = 1560646410L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 8L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            ]
        Cols = [
            ColEntity(
                Id = 1L,
                Crt = 1556096400L,
                Mod = 1560646804961L,
                Scm = 1560646804942L,
                Ver = 11L,
                Dty = 0L,
                Usn = 0L,
                Ls = 0L,
                Conf = "{\"activeDecks\": [1], \"curDeck\": 1, \"newSpread\": 0, \"collapseTime\": 1200, \"timeLim\": 0, \"estTimes\": true, \"dueCounts\": true, \"curModel\": \"1560646804944\", \"nextPos\": 1, \"sortType\": \"noteFld\", \"sortBackwards\": false, \"addToCur\": true, \"dayLearnFirst\": false, \"newBury\": true}",
                Models = "{\"1554689669581\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646410, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [], \"id\": 1554689669581, \"req\": [[0, \"all\", [0]]]}, \"1554689669577\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646288, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (and reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669577, \"req\": [[0, \"all\", [0]], [1, \"all\", [1]]]}, \"1554689669572\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646292, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (optional reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Add Reverse\", \"ord\": 2, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{#Add Reverse}}{{Back}}{{/Add Reverse}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669572, \"req\": [[0, \"all\", [0]], [1, \"all\", [1, 2]]]}, \"1554689669571\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646306, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (type in the answer)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\\n{{type:Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669571, \"req\": [[0, \"all\", [0]]]}, \"1554689669570\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646315, \"usn\": -1, \"vers\": [], \"type\": 1, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\\n.nightMode .cloze {\\n color: lightblue;\\n}\", \"name\": \"Cloze\", \"flds\": [{\"name\": \"Text\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Extra\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Cloze\", \"ord\": 0, \"qfmt\": \"{{cloze:Text}}\", \"afmt\": \"{{cloze:Text}}<br>\\n{{Extra}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669570}}",
                Decks = "{\"1\": {\"newToday\": [0, 0], \"revToday\": [0, 0], \"lrnToday\": [0, 0], \"timeToday\": [0, 0], \"conf\": 1, \"usn\": 0, \"desc\": \"\", \"dyn\": 0, \"collapsed\": false, \"extendNew\": 10, \"extendRev\": 50, \"id\": 1, \"name\": \"Default\", \"mod\": 1560646804}}",
                Dconf = "{\"1\": {\"name\": \"Default\", \"new\": {\"delays\": [1, 10], \"ints\": [1, 4, 7], \"initialFactor\": 2500, \"separate\": true, \"order\": 1, \"perDay\": 20, \"bury\": false}, \"lapse\": {\"delays\": [10], \"mult\": 0, \"minInt\": 1, \"leechFails\": 8, \"leechAction\": 0}, \"rev\": {\"perDay\": 200, \"ease4\": 1.3, \"fuzz\": 0.05, \"minSpace\": 1, \"ivlFct\": 1, \"maxIvl\": 36500, \"bury\": false, \"hardFactor\": 1.2}, \"maxTaken\": 60, \"timer\": 0, \"autoplay\": true, \"replayq\": true, \"mod\": 0, \"usn\": 0, \"id\": 1}}",
                Tags = "{}")
            ]
        Notes = [
            NoteEntity(
                Id = 1554689672054L,
                Guid = "hdW&ZLKq1B",
                Mid = 1554689669581L,
                Mod = 1554689693L,
                Usn = -1L,
                Tags = " Basic Tag ",
                Flds = "Basic FrontBasic Back",
                Sfld = 0L,
                Csum = 3392947020L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689697908L,
                Guid = "Dq.!@mFght",
                Mid = 1554689669577L,
                Mod = 1554689744L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (and reversed card) frontBasic (and reversed card) back",
                Sfld = 0L,
                Csum = 1851683185L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689750007L,
                Guid = "q3-~k#2MqI",
                Mid = 1554689669572L,
                Mod = 1554689782L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (optional reversed card) frontBasic (optional reversed card) backBasic (optional reversed card) reverse",
                Sfld = 0L,
                Csum = 1310466425L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689787164L,
                Guid = "w+4I$EJ/NQ",
                Mid = 1554689669571L,
                Mod = 1554689799L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (type in the answer) frontBasic (type in the answer) back",
                Sfld = 0L,
                Csum = 4281307022L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689802142L,
                Guid = "yuqNm3;_r2",
                Mid = 1554689669570L,
                Mod = 1554689885L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Cloze text.&nbsp;Canberra was founded in {{c1::1913}}.Cloze extra",
                Sfld = 0L,
                Csum = 1283850154L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689900910L,
                Guid = "f_TwXvEN<g",
                Mid = 1554689669581L,
                Mod = 1554691431L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic with image&nbsp;<img src=\"favicon.ico\">Basic back, no image",
                Sfld = 0L,
                Csum = 2183677625L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554690071622L,
                Guid = "xa]y)`6T[0",
                Mid = 1554689669581L,
                Mod = 1554691132L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic front with mp3[sound:bloop.wav]Basic back, no mp3",
                Sfld = 0L,
                Csum = 283440196L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1560646400431L,
                Guid = "s_4H3`+|ZV",
                Mid = 1554689669581L,
                Mod = 1560646587L,
                Usn = -1L,
                Tags = "",
                Flds = "<img src=\"png1.png\"><img src=\"png2.png\">",
                Sfld = 0L,
                Csum = 364157591L,
                Flags = 0L,
                Data = "")
            ]
        Revlogs = []
    }

let allDefaultTemplatesAndImageAndMp3_21_colpkg =
    {
        Cards = [
            CardEntity(
                Id = 1554689693560L,
                Nid = 1554689672054L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 1L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689744008L,
                Nid = 1554689697908L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 2L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689744009L,
                Nid = 1554689697908L,
                Did = 1L,
                Ord = 1L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 2L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689782082L,
                Nid = 1554689750007L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 3L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689782083L,
                Nid = 1554689750007L,
                Did = 1L,
                Ord = 1L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 3L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689799193L,
                Nid = 1554689787164L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 4L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689885405L,
                Nid = 1554689802142L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 5L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554690071566L,
                Nid = 1554689900910L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 6L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554690471678L,
                Nid = 1554690071622L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 7L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1560646410586L,
                Nid = 1560646400431L,
                Did = 1L,
                Ord = 0L,
                Mod = 1560646410L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 8L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            ]
        Cols = [
            ColEntity(
                Id = 1L,
                Crt = 1556096400L,
                Mod = 1560646700734L,
                Scm = 1560646279929L,
                Ver = 11L,
                Dty = 0L,
                Usn = 0L,
                Ls = 0L,
                Conf = "{\"activeDecks\": [1], \"curDeck\": 1, \"newSpread\": 0, \"collapseTime\": 1200, \"timeLim\": 0, \"estTimes\": true, \"dueCounts\": true, \"curModel\": 1554689669581, \"nextPos\": 9, \"sortType\": \"noteFld\", \"sortBackwards\": false, \"addToCur\": true, \"dayLearnFirst\": false, \"newBury\": true, \"activeCols\": [\"noteFld\", \"template\", \"cardDue\", \"deck\"], \"nightMode\": false, \"schedVer\": 2, \"rollover\": 4, \"lastUnburied\": 52, \"previewBothSides\": true}",
                Models = "{\"1554689669581\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646410, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [], \"id\": 1554689669581, \"req\": [[0, \"all\", [0]]]}, \"1554689669577\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646288, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (and reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669577, \"req\": [[0, \"all\", [0]], [1, \"all\", [1]]]}, \"1554689669572\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646292, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (optional reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Add Reverse\", \"ord\": 2, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{#Add Reverse}}{{Back}}{{/Add Reverse}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669572, \"req\": [[0, \"all\", [0]], [1, \"all\", [1, 2]]]}, \"1554689669571\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646306, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (type in the answer)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\\n{{type:Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669571, \"req\": [[0, \"all\", [0]]]}, \"1554689669570\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646315, \"usn\": -1, \"vers\": [], \"type\": 1, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\\n.nightMode .cloze {\\n color: lightblue;\\n}\", \"name\": \"Cloze\", \"flds\": [{\"name\": \"Text\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Extra\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Cloze\", \"ord\": 0, \"qfmt\": \"{{cloze:Text}}\", \"afmt\": \"{{cloze:Text}}<br>\\n{{Extra}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669570}}",
                Decks = "{\"1\": {\"newToday\": [0, 0], \"revToday\": [0, 0], \"lrnToday\": [0, 0], \"timeToday\": [0, 0], \"conf\": 1, \"usn\": -1, \"desc\": \"\", \"dyn\": 0, \"collapsed\": false, \"extendNew\": 10, \"extendRev\": 50, \"id\": 1, \"name\": \"Default\", \"mod\": 1556154677}}",
                Dconf = "{\"1\": {\"name\": \"Default\", \"new\": {\"delays\": [1, 10], \"ints\": [1, 4, 7], \"initialFactor\": 2500, \"separate\": true, \"order\": 1, \"perDay\": 20, \"bury\": false}, \"lapse\": {\"delays\": [10], \"mult\": 0, \"minInt\": 1, \"leechFails\": 8, \"leechAction\": 0}, \"rev\": {\"perDay\": 200, \"ease4\": 1.3, \"fuzz\": 0.05, \"minSpace\": 1, \"ivlFct\": 1, \"maxIvl\": 36500, \"bury\": false, \"hardFactor\": 1.2}, \"maxTaken\": 60, \"timer\": 0, \"autoplay\": true, \"replayq\": true, \"mod\": 0, \"usn\": 0, \"id\": 1, \"dyn\": false}}",
                Tags = "{\"Basic\": -1, \"OtherTag\": -1, \"Tag\": -1}")
            ]
        Notes = [
            NoteEntity(
                Id = 1554689672054L,
                Guid = "hdW&ZLKq1B",
                Mid = 1554689669581L,
                Mod = 1554689693L,
                Usn = -1L,
                Tags = " Basic Tag ",
                Flds = "Basic FrontBasic Back",
                Sfld = 0L,
                Csum = 3392947020L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689697908L,
                Guid = "Dq.!@mFght",
                Mid = 1554689669577L,
                Mod = 1554689744L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (and reversed card) frontBasic (and reversed card) back",
                Sfld = 0L,
                Csum = 1851683185L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689750007L,
                Guid = "q3-~k#2MqI",
                Mid = 1554689669572L,
                Mod = 1554689782L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (optional reversed card) frontBasic (optional reversed card) backBasic (optional reversed card) reverse",
                Sfld = 0L,
                Csum = 1310466425L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689787164L,
                Guid = "w+4I$EJ/NQ",
                Mid = 1554689669571L,
                Mod = 1554689799L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (type in the answer) frontBasic (type in the answer) back",
                Sfld = 0L,
                Csum = 4281307022L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689802142L,
                Guid = "yuqNm3;_r2",
                Mid = 1554689669570L,
                Mod = 1554689885L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Cloze text.&nbsp;Canberra was founded in {{c1::1913}}.Cloze extra",
                Sfld = 0L,
                Csum = 1283850154L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689900910L,
                Guid = "f_TwXvEN<g",
                Mid = 1554689669581L,
                Mod = 1554691431L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic with image&nbsp;<img src=\"favicon.ico\">Basic back, no image",
                Sfld = 0L,
                Csum = 2183677625L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554690071622L,
                Guid = "xa]y)`6T[0",
                Mid = 1554689669581L,
                Mod = 1554691132L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic front with mp3[sound:bloop.wav]Basic back, no mp3",
                Sfld = 0L,
                Csum = 283440196L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1560646400431L,
                Guid = "s_4H3`+|ZV",
                Mid = 1554689669581L,
                Mod = 1560646587L,
                Usn = -1L,
                Tags = "",
                Flds = "<img src=\"png1.png\"><img src=\"png2.png\">",
                Sfld = 0L,
                Csum = 364157591L,
                Flags = 0L,
                Data = "")
            ]
        Revlogs = []
    }

let allDefaultTemplatesAndImageAndMp3_colpkg =
    {
        Cards = [
            CardEntity(
                Id = 1554689693560L,
                Nid = 1554689672054L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 1L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689744008L,
                Nid = 1554689697908L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 2L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689744009L,
                Nid = 1554689697908L,
                Did = 1L,
                Ord = 1L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 2L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689782082L,
                Nid = 1554689750007L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 3L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689782083L,
                Nid = 1554689750007L,
                Did = 1L,
                Ord = 1L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 3L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689799193L,
                Nid = 1554689787164L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 4L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689885405L,
                Nid = 1554689802142L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 5L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554690071566L,
                Nid = 1554689900910L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 6L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554690471678L,
                Nid = 1554690071622L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 7L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1560646410586L,
                Nid = 1560646400431L,
                Did = 1L,
                Ord = 0L,
                Mod = 1560646410L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 8L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            ]
        Cols = [
            ColEntity(
                Id = 1L,
                Crt = 1556096400L,
                Mod = 1560646793967L,
                Scm = 1560646779786L,
                Ver = 11L,
                Dty = 0L,
                Usn = 0L,
                Ls = 0L,
                Conf = "{\"activeDecks\": [1], \"curDeck\": 1, \"newSpread\": 0, \"collapseTime\": 1200, \"timeLim\": 0, \"estTimes\": true, \"dueCounts\": true, \"curModel\": 1554689669581, \"nextPos\": 9, \"sortType\": \"noteFld\", \"sortBackwards\": false, \"addToCur\": true, \"dayLearnFirst\": false, \"newBury\": true, \"activeCols\": [\"noteFld\", \"template\", \"cardDue\", \"deck\"], \"nightMode\": false, \"schedVer\": 1, \"rollover\": 4, \"lastUnburied\": 52, \"previewBothSides\": true}",
                Models = "{\"1554689669581\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646410, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [], \"id\": 1554689669581, \"req\": [[0, \"all\", [0]]]}, \"1554689669577\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646288, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (and reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669577, \"req\": [[0, \"all\", [0]], [1, \"all\", [1]]]}, \"1554689669572\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646292, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (optional reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Add Reverse\", \"ord\": 2, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{#Add Reverse}}{{Back}}{{/Add Reverse}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669572, \"req\": [[0, \"all\", [0]], [1, \"all\", [1, 2]]]}, \"1554689669571\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646306, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (type in the answer)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\\n{{type:Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669571, \"req\": [[0, \"all\", [0]]]}, \"1554689669570\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646315, \"usn\": -1, \"vers\": [], \"type\": 1, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\\n.nightMode .cloze {\\n color: lightblue;\\n}\", \"name\": \"Cloze\", \"flds\": [{\"name\": \"Text\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Extra\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Cloze\", \"ord\": 0, \"qfmt\": \"{{cloze:Text}}\", \"afmt\": \"{{cloze:Text}}<br>\\n{{Extra}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669570}}",
                Decks = "{\"1\": {\"newToday\": [0, 0], \"revToday\": [0, 0], \"lrnToday\": [0, 0], \"timeToday\": [0, 0], \"conf\": 1, \"usn\": -1, \"desc\": \"\", \"dyn\": 0, \"collapsed\": false, \"extendNew\": 10, \"extendRev\": 50, \"id\": 1, \"name\": \"Default\", \"mod\": 1556154677}}",
                Dconf = "{\"1\": {\"name\": \"Default\", \"new\": {\"delays\": [1, 10], \"ints\": [1, 4, 7], \"initialFactor\": 2500, \"separate\": true, \"order\": 1, \"perDay\": 20, \"bury\": false}, \"lapse\": {\"delays\": [10], \"mult\": 0, \"minInt\": 1, \"leechFails\": 8, \"leechAction\": 0}, \"rev\": {\"perDay\": 200, \"ease4\": 1.3, \"fuzz\": 0.05, \"minSpace\": 1, \"ivlFct\": 1, \"maxIvl\": 36500, \"bury\": false, \"hardFactor\": 1.2}, \"maxTaken\": 60, \"timer\": 0, \"autoplay\": true, \"replayq\": true, \"mod\": 0, \"usn\": 0, \"id\": 1, \"dyn\": false}}",
                Tags = "{\"Basic\": -1, \"OtherTag\": -1, \"Tag\": -1}")
            ]
        Notes = [
            NoteEntity(
                Id = 1554689672054L,
                Guid = "hdW&ZLKq1B",
                Mid = 1554689669581L,
                Mod = 1554689693L,
                Usn = -1L,
                Tags = " Basic Tag ",
                Flds = "Basic FrontBasic Back",
                Sfld = 0L,
                Csum = 3392947020L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689697908L,
                Guid = "Dq.!@mFght",
                Mid = 1554689669577L,
                Mod = 1554689744L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (and reversed card) frontBasic (and reversed card) back",
                Sfld = 0L,
                Csum = 1851683185L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689750007L,
                Guid = "q3-~k#2MqI",
                Mid = 1554689669572L,
                Mod = 1554689782L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (optional reversed card) frontBasic (optional reversed card) backBasic (optional reversed card) reverse",
                Sfld = 0L,
                Csum = 1310466425L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689787164L,
                Guid = "w+4I$EJ/NQ",
                Mid = 1554689669571L,
                Mod = 1554689799L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (type in the answer) frontBasic (type in the answer) back",
                Sfld = 0L,
                Csum = 4281307022L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689802142L,
                Guid = "yuqNm3;_r2",
                Mid = 1554689669570L,
                Mod = 1554689885L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Cloze text.&nbsp;Canberra was founded in {{c1::1913}}.Cloze extra",
                Sfld = 0L,
                Csum = 1283850154L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689900910L,
                Guid = "f_TwXvEN<g",
                Mid = 1554689669581L,
                Mod = 1554691431L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic with image&nbsp;<img src=\"favicon.ico\">Basic back, no image",
                Sfld = 0L,
                Csum = 2183677625L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554690071622L,
                Guid = "xa]y)`6T[0",
                Mid = 1554689669581L,
                Mod = 1554691132L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic front with mp3[sound:bloop.wav]Basic back, no mp3",
                Sfld = 0L,
                Csum = 283440196L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1560646400431L,
                Guid = "s_4H3`+|ZV",
                Mid = 1554689669581L,
                Mod = 1560646587L,
                Usn = -1L,
                Tags = "",
                Flds = "<img src=\"png1.png\"><img src=\"png2.png\">",
                Sfld = 0L,
                Csum = 364157591L,
                Flags = 0L,
                Data = "")
            ]
        Revlogs = []
    }

type AllDefaultTemplatesAndImageAndMp3 () =
    inherit XunitClassDataBase
        ([  [|"AllDefaultTemplatesAndImageAndMp3-21.colpkg" ; allDefaultTemplatesAndImageAndMp3_21_colpkg |]
            [|"AllDefaultTemplatesAndImageAndMp3.colpkg" ; allDefaultTemplatesAndImageAndMp3_colpkg |]
            [|"AllDefaultTemplatesAndImageAndMp3.apkg" ; allDefaultTemplatesAndImageAndMp3_apkg |] ])

let fileEntityByAnkiFileName () =
    [ ("png1.png", Array.init 32 (fun index -> index + index |> byte))
      ("favicon.ico", Array.init 32 (fun index -> index * index |> byte))
      ("bloop.wav", Array.init 32 (fun index -> index * index + index |> byte))
    ] |> List.map (fun (name, hash) ->
    (   name,
        CardOverflow.Entity.FileEntity(
            Sha256 = hash,
            Data = Array.zeroCreate 0,
            FileName = name
        )
    ))
    |> fun files ->
        let _, png1entity = files.[0]
        ("png2.png", png1entity) :: files
    |> Map.ofList

let ankiExportsDir = Directory.GetCurrentDirectory() +/ "AnkiExports"

let ankiDb ankiFileName callerMemberName =
    let unzipDir = ankiExportsDir +/ "Temp" +/ ankiFileName + callerMemberName // Need to isolate each tests's ankiDb otherwise tests run in parallel fail
    if Directory.Exists unzipDir
    then Directory.Delete(unzipDir, true)
    Directory.CreateDirectory unzipDir |> ignore
    Anki.unzipCollectionToRandom (ankiExportsDir +/ ankiFileName) unzipDir
    |> AnkiDbFactory.Create

let serialize x =
    ObjectDumper.Dump(x, DumpOptions(DumpStyle = DumpStyle.CSharp,
                                     LineBreakChar = "",
                                     IndentSize = 0))

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Actual AllDefaultTemplatesAndImageAndMp3 matches mock`` fileName mock =
    let actualDb =
        AnkiImporter.getSimpleAnkiDb
        |> using(ankiDb fileName "Actual testnametoolong matches mock")
    
    serialize actualDb.Revlogs = serialize mock.Revlogs |> Assert.True
    serialize actualDb.Cols = serialize mock.Cols |> Assert.True
    serialize actualDb.Notes = serialize mock.Notes |> Assert.True
    serialize actualDb.Cards = serialize mock.Cards |> Assert.True

type AnkiTestContainer(ankiFileName: string, [<CallerMemberName>] ?memberName: string) =
    let container = new TestContainer(ankiFileName, memberName.Value)
    interface IDisposable with
        member __.Dispose() =
            (container :> IDisposable).Dispose()
    member __.Db =
        container.Db
    member __.AnkiDb () =
        ankiDb ankiFileName memberName.Value

type AllAnkiCols () =
    inherit XunitClassDataBase
        ([  [|"basic"; """{"1": {"name": "Default", "replayq": true, "lapse": {"leechFails": 8, "minInt": 1, "delays": [10], "leechAction": 0, "mult": 0.0}, "rev": {"ivlFct": 1.0, "perDay": 200, "hardFactor": 1.2, "maxIvl": 36500, "minSpace": 1, "ease4": 1.3, "bury": false, "fuzz": 0.05}, "timer": 0, "dyn": false, "maxTaken": 60, "usn": 2207, "new": {"separate": true, "delays": [1, 10], "perDay": 20, "ints": [1, 4, 7], "initialFactor": 2500, "bury": true, "order": 1}, "autoplay": true, "id": 1, "mod": 1556388151}}""" |]
            [|"lightyear"; """{"1": {"name": "Default", "replayq": true, "lapse": {"leechFails": 8, "minInt": 1, "delays": [10], "leechAction": 0, "mult": 0}, "rev": {"perDay": 100, "fuzz": 0.05, "ivlFct": 1, "maxIvl": 36500, "ease4": 1.3, "bury": true, "minSpace": 1}, "timer": 0, "maxTaken": 60, "usn": 0, "new": {"perDay": 20, "delays": [1, 10], "separate": true, "ints": [1, 4, 7], "initialFactor": 2500, "bury": true, "order": 1}, "mod": 0, "id": 1, "autoplay": true}}""" |] ])

[<Theory>]
[<ClassData(typeof<AllAnkiCols>)>]
let ``parseCardOptions`` title col =
    Anki.parseCardOptions col
    |> Result.isOk
    |> Assert.True

let duplicatesFromLightyear =
    {
        Cards = [
            CardEntity(
                Id = 1520552885002L,
                Nid = 1520552858412L,
                Did = 1565233621290L,
                Ord = 0L,
                Mod = 1565233621L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 11526L,
                Ivl = 0L,
                Factor = 2500L,
                Reps = 0L,
                Lapses = 0L,
                Left = 1001L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1521639929120L,
                Nid = 1521639926507L,
                Did = 1565233621290L,
                Ord = 0L,
                Mod = 1565233621L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 12585L,
                Ivl = 0L,
                Factor = 2500L,
                Reps = 0L,
                Lapses = 0L,
                Left = 1001L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1522269365967L,
                Nid = 1522269363931L,
                Did = 1565233621290L,
                Ord = 0L,
                Mod = 1565233621L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 13327L,
                Ivl = 0L,
                Factor = 2500L,
                Reps = 0L,
                Lapses = 0L,
                Left = 1001L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")]
        Cols = [
            ColEntity(
                Id = 1L,
                Crt = 1558170000L,
                Mod = 1565233639633L,
                Scm = 1565233639306L,
                Ver = 11L,
                Dty = 0L,
                Usn = 0L,
                Ls = 0L,
                Conf = "{\"nextPos\": 1, \"estTimes\": true, \"activeDecks\": [1], \"sortType\": \"noteFld\", \"timeLim\": 0, \"sortBackwards\": false, \"addToCur\": true, \"curDeck\": 1, \"newBury\": true, \"newSpread\": 0, \"dueCounts\": true, \"curModel\": \"1565233639307\", \"collapseTime\": 1200}",
                Models = "{\"1481327979421\": {\"vers\": [], \"name\": \"Basic-95fd5\", \"tags\": [], \"did\": 1529062737991, \"usn\": -1, \"req\": [[0, \"all\", [0]]], \"flds\": [{\"name\": \"Front\", \"media\": [], \"sticky\": false, \"rtl\": false, \"ord\": 0, \"font\": \"Arial\", \"size\": 20}, {\"name\": \"Back\", \"media\": [], \"sticky\": false, \"rtl\": false, \"ord\": 1, \"font\": \"Arial\", \"size\": 20}], \"sortf\": 0, \"tmpls\": [{\"name\": \"Card 1\", \"qfmt\": \"{{Front}}\", \"did\": null, \"bafmt\": \"\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"ord\": 0, \"bqfmt\": \"\"}], \"mod\": 1565231941, \"latexPost\": \"\\\\end{document}\", \"type\": 0, \"id\": 1481327979421, \"css\": \".card {\\n font-family:Arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color:#F2F6FF;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\\n\\n.card:after {\\n    content: \\\"\\\";\\n    background: url(Logo.png);\\n    background-size: 80px;\\n    background-position: right bottom;\\n    background-repeat: no-repeat;\\n    background-color: ;\\n    opacity: 0.7;\\n    top: 0;\\n    left: 0;\\n    bottom: 0;\\n    right: 0;\\n    position: absolute;\\n    z-index: -1;     \\n}\", \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\"}}",
                Decks = "{\"1\": {\"desc\": \"\", \"name\": \"Default\", \"extendRev\": 50, \"usn\": 0, \"collapsed\": false, \"newToday\": [0, 0], \"timeToday\": [0, 0], \"dyn\": 0, \"extendNew\": 10, \"conf\": 1, \"revToday\": [0, 0], \"lrnToday\": [0, 0], \"id\": 1, \"mod\": 1565233638}, \"1565233621290\": {\"name\": \"duplicate cards\", \"extendRev\": 50, \"usn\": -1, \"collapsed\": false, \"browserCollapsed\": true, \"newToday\": [81, 0], \"timeToday\": [81, 0], \"dyn\": 0, \"extendNew\": 10, \"conf\": 1, \"revToday\": [81, 0], \"lrnToday\": [81, 0], \"id\": 1565233621290, \"mod\": 1565233621, \"desc\": \"\"}}",
                Dconf = "{\"1\": {\"name\": \"Default\", \"replayq\": true, \"lapse\": {\"leechFails\": 8, \"minInt\": 1, \"delays\": [10], \"leechAction\": 0, \"mult\": 0}, \"rev\": {\"perDay\": 200, \"fuzz\": 0.05, \"ivlFct\": 1, \"maxIvl\": 36500, \"ease4\": 1.3, \"bury\": false, \"minSpace\": 1}, \"timer\": 0, \"maxTaken\": 60, \"usn\": 0, \"new\": {\"perDay\": 20, \"delays\": [1, 10], \"separate\": true, \"ints\": [1, 4, 7], \"initialFactor\": 2500, \"bury\": false, \"order\": 1}, \"mod\": 0, \"id\": 1, \"autoplay\": true}}",
                Tags = "{}")]
        Notes = [
            NoteEntity(
                Id = 1520552858412L,
                Guid = "PR(e2Y+qaE",
                Mid = 1481327979421L,
                Mod = 1521640339L,
                Usn = -1L,
                Tags = " Pathoma::Neoplasia::Tumor_Progression ",
                Flds = "Which carcinomas spread hematogenously?<div>Follicular thyroid carcinoma</div><div>Choriocarcinoma</div><div>Renal cell carcinoma</div><div>Hepatocellular carcinoma</div><div><br /></div><div>Image here</div>",
                Sfld = 0L,
                Csum = 819872869L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1521639926507L,
                Guid = "BO<KxLp7;%",
                Mid = 1481327979421L,
                Mod = 1524534523L,
                Usn = -1L,
                Tags = " bab::gastroenterology::clinical::livertumors repeatedTag ",
                Flds = "Which carcinomas spread hematogenously?<div>Follicular thyroid carcinoma</div><div>Choriocarcinoma</div><div>Renal cell carcinoma</div><div>Hepatocellular carcinoma</div><div><br /></div><div>Image here</div>",
                Sfld = 0L,
                Csum = 819872869L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1522269363931L,
                Guid = "oAIozlbsaq",
                Mid = 1481327979421L,
                Mod = 1524711255L,
                Usn = -1L,
                Tags = " bab::endocrinology::thyroid::thyroidcancer repeatedTag ",
                Flds = "Which carcinomas spread hematogenously?<div>Follicular thyroid carcinoma</div><div>Choriocarcinoma</div><div>Renal cell carcinoma</div><div>Hepatocellular carcinoma</div><div><br /></div><div>Image here</div>",
                Sfld = 0L,
                Csum = 819872869L,
                Flags = 0L,
                Data = "")]
        Revlogs = []
    }

let multipleClozeAndSingleClozeAndNoClozeWithMissingImage =
    {
        Cards = [
            CardEntity(
                Id = 1468250104706L,
                Nid = 1468250087836L,
                Did = 1565397508807L,
                Ord = 0L,
                Mod = 1565397525L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 1440286636L,
                Ivl = 0L,
                Factor = 2500L,
                Reps = 0L,
                Lapses = 0L,
                Left = 1001L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1476123765096L,
                Nid = 1468250087836L,
                Did = 1565397508807L,
                Ord = 1L,
                Mod = 1565397525L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 1440286636L,
                Ivl = 0L,
                Factor = 2500L,
                Reps = 0L,
                Lapses = 0L,
                Left = 1001L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1476123767576L,
                Nid = 1468250087836L,
                Did = 1565397508807L,
                Ord = 2L,
                Mod = 1565397525L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 1440286636L,
                Ivl = 0L,
                Factor = 2500L,
                Reps = 0L,
                Lapses = 0L,
                Left = 1001L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1476123770016L,
                Nid = 1468250087836L,
                Did = 1565397508807L,
                Ord = 3L,
                Mod = 1565397525L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 1440286636L,
                Ivl = 0L,
                Factor = 2500L,
                Reps = 0L,
                Lapses = 0L,
                Left = 1001L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1476123773222L,
                Nid = 1468250087836L,
                Did = 1565397508807L,
                Ord = 4L,
                Mod = 1565397525L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 1440286636L,
                Ivl = 0L,
                Factor = 2500L,
                Reps = 0L,
                Lapses = 0L,
                Left = 1002L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1512403275238L,
                Nid = 1512403195942L,
                Did = 1565397508807L,
                Ord = 0L,
                Mod = 1565398091L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 5650L,
                Ivl = 0L,
                Factor = 2500L,
                Reps = 0L,
                Lapses = 0L,
                Left = 2003L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1523138237877L,
                Nid = 1523138213639L,
                Did = 1565397508807L,
                Ord = 0L,
                Mod = 1565398039L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 14048L,
                Ivl = 0L,
                Factor = 2500L,
                Reps = 0L,
                Lapses = 0L,
                Left = 1001L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")]
        Cols = [
            ColEntity(
                Id = 1L,
                Crt = 1558170000L,
                Mod = 1565398127890L,
                Scm = 1565398127840L,
                Ver = 11L,
                Dty = 0L,
                Usn = 0L,
                Ls = 0L,
                Conf = "{\"nextPos\": 1, \"estTimes\": true, \"activeDecks\": [1], \"sortType\": \"noteFld\", \"timeLim\": 0, \"sortBackwards\": false, \"addToCur\": true, \"curDeck\": 1, \"newBury\": true, \"newSpread\": 0, \"dueCounts\": true, \"curModel\": \"1565398127842\", \"collapseTime\": 1200}",
                Models = "{\"1358629116480\": {\"vers\": [], \"name\": \"Cloze-88141\", \"tags\": [\"PathomaRespiratory\", \"RespiratoryPathology\", \"cancer\", \"lung_cancer\"], \"did\": 1478521906656, \"usn\": -1, \"flds\": [{\"name\": \"Text\", \"rtl\": false, \"sticky\": false, \"media\": [], \"ord\": 0, \"font\": \"Helvetica\", \"size\": 20}, {\"name\": \"Extra\", \"rtl\": false, \"sticky\": false, \"media\": [], \"ord\": 1, \"font\": \"Helvetica\", \"size\": 20}], \"sortf\": 0, \"tmpls\": [{\"name\": \"Cloze\", \"qfmt\": \"{{cloze:Text}}\", \"bsize\": 12, \"bafmt\": \"\", \"did\": null, \"afmt\": \"{{cloze:Text}}<br>\\n{{Extra}}\", \"bfont\": \"Arial\", \"ord\": 0, \"bqfmt\": \"\"}], \"mod\": 1565397478, \"latexPost\": \"\\\\end{document}\", \"type\": 1, \"id\": 1358629116480, \"css\": \".card {\\n font-family: Times New Roman;\\n font-size: 18px;\\n text-align: center;\\n color: black;\\n background-color: lavender;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\", \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\"}, \"1451282465113\": {\"vers\": [], \"name\": \"Cloze-Lightyear\", \"tags\": [\"Uworld\"], \"did\": 1507998105695, \"usn\": -1, \"flds\": [{\"name\": \"Text\", \"media\": [], \"sticky\": false, \"rtl\": false, \"ord\": 0, \"font\": \"Arial\", \"size\": 20}, {\"name\": \"Extra\", \"media\": [], \"sticky\": false, \"rtl\": false, \"ord\": 1, \"font\": \"Arial\", \"size\": 20}], \"sortf\": 0, \"tmpls\": [{\"name\": \"Cloze\", \"qfmt\": \"{{cloze:Text}}\", \"did\": null, \"bafmt\": \"\", \"afmt\": \"{{cloze:Text}}<br>\\n\\n\\n{{Extra}}\\n\", \"ord\": 0, \"bqfmt\": \"\"}], \"mod\": 1565231941, \"latexPost\": \"\\\\end{document}\", \"type\": 1, \"id\": 1451282465113, \"css\": \".card {\\n font-family:Arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color:#F2F6FF;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\\n\\n.card:after {\\n    content: \\\"\\\";\\n    background: url(Logo.png);\\n    background-size: 80px;\\n    background-position: right bottom;\\n    background-repeat: no-repeat;\\n    background-color: ;\\n    opacity: 0.7;\\n    top: 0;\\n    left: 0;\\n    bottom: 0;\\n    right: 0;\\n    position: absolute;\\n    z-index: -1;     \\n}\", \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\"}, \"1481327979421\": {\"vers\": [], \"name\": \"Basic-95fd5\", \"tags\": [], \"did\": 1529062737991, \"usn\": -1, \"req\": [[0, \"all\", [0]]], \"flds\": [{\"name\": \"Front\", \"media\": [], \"sticky\": false, \"rtl\": false, \"ord\": 0, \"font\": \"Arial\", \"size\": 20}, {\"name\": \"Back\", \"media\": [], \"sticky\": false, \"rtl\": false, \"ord\": 1, \"font\": \"Arial\", \"size\": 20}], \"sortf\": 0, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"tmpls\": [{\"name\": \"Card 1\", \"qfmt\": \"{{Front}}\", \"did\": null, \"bafmt\": \"\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"ord\": 0, \"bqfmt\": \"\"}], \"latexPost\": \"\\\\end{document}\", \"type\": 0, \"id\": 1481327979421, \"css\": \".card {\\n font-family:Arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color:#F2F6FF;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\\n\\n.card:after {\\n    content: \\\"\\\";\\n    background: url(Logo.png);\\n    background-size: 80px;\\n    background-position: right bottom;\\n    background-repeat: no-repeat;\\n    background-color: ;\\n    opacity: 0.7;\\n    top: 0;\\n    left: 0;\\n    bottom: 0;\\n    right: 0;\\n    position: absolute;\\n    z-index: -1;     \\n}\", \"mod\": 1565231941}}",
                Decks = "{\"1\": {\"desc\": \"\", \"name\": \"Default\", \"extendRev\": 50, \"usn\": 0, \"collapsed\": false, \"newToday\": [0, 0], \"timeToday\": [0, 0], \"dyn\": 0, \"extendNew\": 10, \"conf\": 1, \"revToday\": [0, 0], \"lrnToday\": [0, 0], \"id\": 1, \"mod\": 1565398127}, \"1565397508807\": {\"name\": \"MultipleCloze\", \"extendRev\": 50, \"usn\": -1, \"collapsed\": false, \"browserCollapsed\": true, \"newToday\": [83, 0], \"timeToday\": [83, 0], \"dyn\": 0, \"extendNew\": 10, \"conf\": 1, \"revToday\": [83, 0], \"lrnToday\": [83, 0], \"id\": 1565397508807, \"mod\": 1565397508, \"desc\": \"\"}}",
                Dconf = "{\"1\": {\"name\": \"Default\", \"replayq\": true, \"lapse\": {\"leechFails\": 8, \"minInt\": 1, \"delays\": [10], \"leechAction\": 0, \"mult\": 0}, \"rev\": {\"perDay\": 200, \"fuzz\": 0.05, \"ivlFct\": 1, \"maxIvl\": 36500, \"ease4\": 1.3, \"bury\": false, \"minSpace\": 1}, \"timer\": 0, \"maxTaken\": 60, \"usn\": 0, \"new\": {\"perDay\": 20, \"delays\": [1, 10], \"separate\": true, \"ints\": [1, 4, 7], \"initialFactor\": 2500, \"bury\": false, \"order\": 1}, \"mod\": 0, \"id\": 1, \"autoplay\": true}}",
                Tags = "{}")]
        Notes = [
            NoteEntity(
                Id = 1468250087836L,
                Guid = "tcGjwyUho8",
                Mid = 1358629116480L,
                Mod = 1476123780L,
                Usn = -1L,
                Tags = " CellularBiochem round1 ",
                Flds = "<div>&nbsp;<u>Drugs</u>&nbsp;that act on&nbsp;<b>microtubules</b>&nbsp;may be remembered with the mnemonic \"<i><b>M</b>icrotubules&nbsp;<b>G</b>et&nbsp;<b>C</b>onstructed&nbsp;<b>V</b>ery&nbsp;<b>P</b>oorly</i>\":</div><div><br /></div><div><b>M</b>:&nbsp;{{c1::Mebendazole (antihelminthic)}}<div><b>G</b>:&nbsp;{{c2::Griseofulvin (antifungal)}}&nbsp;</div><div><b>C</b>:&nbsp;{{c3::Colchicine (antigout)}}&nbsp;</div><div><b>V</b>:&nbsp;{{c4::Vincristine/Vinblastine (anticancer)}}</div><div><b>P</b>:&nbsp;{{c5::Palcitaxel (anticancer)}}&nbsp;</div><br /></div><b><br /></b>",
                Sfld = 0L,
                Csum = 1619316023L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1512403195942L,
                Guid = "DzdVu*.pI+",
                Mid = 1451282465113L,
                Mod = 1526180107L,
                Usn = -1L,
                Tags = " bab::pulmonary::other::cysticfibrosis ",
                Flds = "↑&nbsp;{{c1::Cl<sup>−</sup>}}&nbsp;concentration (&gt; 60 mEq/L) in sweat is diagnostic for Cystic Fibrosis<br /><div><br /></div><div>Image here</div>",
                Sfld = 0L,
                Csum = 3784927019L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1523138213639L,
                Guid = "z8^GIYA9V.",
                Mid = 1481327979421L,
                Mod = 1526696721L,
                Usn = -1L,
                Tags = " Pathoma::Kidney::Acute_Renal_Failure ",
                Flds = "↑↑ BUN/CR ratio indicates which type of acute renal failure?Prerenal azotemia<img src=\"missingImage.png\">",
                Sfld = 0L,
                Csum = 1602079430L,
                Flags = 0L,
                Data = "")]
        Revlogs = []
    }