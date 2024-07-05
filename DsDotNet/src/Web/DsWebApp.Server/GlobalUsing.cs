global using System.Data.Common;
global using System.Diagnostics;
global using System.Reactive.Linq;
global using System.Reactive.Disposables;
global using Microsoft.AspNetCore.SignalR;
global using Dapper;
global using Microsoft.AspNetCore.Mvc;
global using log4net;


global using DsWebApp.Shared;
global using DsWebApp.Server.Common;
global using Dual.Common.Core;
global using Dual.Common.Db;
global using Dual.Web.Server;
global using DsWebApp.Server.Hubs;
global using Dual.Web.Server.Controllers;
global using Dual.Web.Blazor.Shared;




global using ErrorMessage = string;
global using NewtonsoftJson = Newtonsoft.Json.JsonConvert;
global using SystemTextJson = System.Text.Json.JsonSerializer;
