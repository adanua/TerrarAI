﻿using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class ThoriumMediaWiki : BaseMediaWikiGG
{
    public ThoriumMediaWiki()
    {
        LoadWiki("thoriummod");
    }
}
