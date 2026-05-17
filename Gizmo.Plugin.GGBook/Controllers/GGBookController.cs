using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gizmo.Plugin.GGBook.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Gizmo.Plugin.GGBook.Controllers
{
    [ApiController]
    [Route("api/ggbook")]
    [ApiKeyAuth]
    public class GGBookController : ControllerBase
    {
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string ServiceJsonPath = Path.Combine(BaseDir, "Service.json");
        private static readonly string SkinDir = ResolveSkinDir();

        // ── Basic endpoints ─────────────────────────────────────────────────────

        [HttpGet("ping")]
        public IActionResult Ping() =>
            Ok(new { status = "ok", message = "Hello from GGBook plugin!", timestamp = DateTimeOffset.UtcNow });

        [HttpGet("versions")]
        public IActionResult Versions()
        {
            var targets = new[] { "Gizmo.Client.UI", "Gizmo.Client.UI.GGMod" };
            return Ok(new
            {
                timestamp = DateTimeOffset.UtcNow,
                skinDir   = SkinDir,
                dlls      = targets.Select(n => BuildEntry(n, SkinDir)).ToList()
            });
        }

        // ── Port sync ───────────────────────────────────────────────────────────

        [HttpPost("sync-port")]
        public IActionResult SyncPort()
        {
            try
            {
                var port = ReadServiceJsonPort();
                var compositionPath = Path.Combine(SkinDir, "composition.json");
                PatchCompositionJson(compositionPath, ggmod =>
                {
                    ggmod["ServerPort"] = port.ToString();
                });

                return Ok(new { status = "ok", port, compositionPath, message = $"GGMod:ServerPort updated to {port}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        // ── Config (UserToken / ClubToken) ──────────────────────────────────────

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                var root  = ParseJson(Path.Combine(SkinDir, "composition.json"));
                var ggmod = root?["GGMod"] as JsonObject;

                return Ok(new
                {
                    status     = "ok",
                    userToken  = ggmod?["UserToken"]?.GetValue<string>(),
                    clubToken  = ggmod?["ClubToken"]?.GetValue<string>(),
                    serverPort = ggmod?["ServerPort"]?.GetValue<string>()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        [HttpPost("config")]
        public IActionResult SetConfig([FromBody] ConfigRequest req)
        {
            try
            {
                PatchCompositionJson(Path.Combine(SkinDir, "composition.json"), ggmod =>
                {
                    if (req.UserToken is not null) ggmod["UserToken"] = req.UserToken;
                    if (req.ClubToken is not null) ggmod["ClubToken"] = req.ClubToken;
                });

                return Ok(new { status = "ok", message = "Config saved to composition.json" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static string ResolveSkinDir()
        {
            var skinName = "Next";
            try
            {
                var root = ParseJson(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Service.json"));
                if (root?["Client"]?["Shell"]?["SkinName"]?.GetValue<string>() is { } name)
                    skinName = name;
            }
            catch { }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins", skinName);
        }

        private static int ReadServiceJsonPort()
        {
            var root = ParseJson(ServiceJsonPath)
                ?? throw new InvalidOperationException("Cannot read Service.json");

            if (root["Service"]?["Web"]?["WebPortalPort"]?.GetValue<int>() is int port)
                return port;

            throw new InvalidOperationException("Service.Web.WebPortalPort not found in Service.json");
        }

        private static JsonNode? ParseJson(string path)
        {
            var text = System.IO.File.ReadAllText(path);
            return JsonNode.Parse(text, null, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        }

        private static void PatchCompositionJson(string path, Action<JsonObject> patch)
        {
            var root = ParseJson(path) as JsonObject
                ?? throw new InvalidOperationException($"Cannot parse {path}");

            if (!root.ContainsKey("GGMod"))
                root["GGMod"] = new JsonObject();

            patch(root["GGMod"]!.AsObject());

            System.IO.File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private static object BuildEntry(string assemblyName, string skinDir)
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (loaded is not null)
            {
                return new
                {
                    dll          = assemblyName + ".dll",
                    source       = "loaded",
                    version      = loaded.GetName().Version?.ToString(),
                    infoVersion  = loaded.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                    fileVersion  = loaded.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version,
                    ggmodPatched = GetMeta(loaded, "GGMod.Patched") == "true",
                    ggmodVersion = GetMeta(loaded, "GGMod.Version"),
                };
            }

            var filePath = Path.Combine(skinDir, assemblyName + ".dll");
            if (System.IO.File.Exists(filePath))
            {
                var fvi = FileVersionInfo.GetVersionInfo(filePath);
                return new
                {
                    dll          = assemblyName + ".dll",
                    source       = "file",
                    version      = (string?)null,
                    infoVersion  = fvi.ProductVersion,
                    fileVersion  = fvi.FileVersion,
                    ggmodPatched = (bool?)null,
                    ggmodVersion = (string?)null,
                };
            }

            return new
            {
                dll          = assemblyName + ".dll",
                source       = "not_found",
                version      = (string?)null,
                infoVersion  = (string?)null,
                fileVersion  = (string?)null,
                ggmodPatched = (bool?)null,
                ggmodVersion = (string?)null,
            };
        }

        private static string GetMeta(Assembly asm, string key)
        {
            foreach (var attr in asm.GetCustomAttributes<AssemblyMetadataAttribute>())
                if (attr.Key == key) return attr.Value ?? string.Empty;
            return string.Empty;
        }
    }

    public sealed class ConfigRequest
    {
        public string? UserToken { get; set; }
        public string? ClubToken { get; set; }
    }
}
