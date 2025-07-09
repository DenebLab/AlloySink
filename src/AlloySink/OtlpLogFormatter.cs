using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Deneblab.AlloySink;

public static class OtlpLogFormatter
{
    public static string FormatLogs(IEnumerable<LogEntry> logEntries)
    {
        var logs = logEntries.ToList();
        if (!logs.Any()) return string.Empty;

        var firstLog = logs.First();

        var otlpLog = new
        {
            resourceLogs = new[]
            {
                new
                {
                    resource = new
                    {
                        attributes = new[]
                        {
                            new { key = "service.name", value = new { stringValue = firstLog.ServiceName } },
                            new { key = "service.version", value = new { stringValue = firstLog.ServiceVersion } },
                            new { key = "environment", value = new { stringValue = firstLog.Environment } }
                        }
                    },
                    scopeLogs = new[]
                    {
                        new
                        {
                            scope = new { name = "AlloySink" },
                            logRecords = logs.Select(log => new
                            {
                                timeUnixNano = (((DateTimeOffset)log.Timestamp).ToUnixTimeMilliseconds() * 1_000_000).ToString(),
                                severityText = GetSeverityText(log.Level),
                                severityNumber = GetSeverityNumber(log.Level),
                                body = new { stringValue = log.Message },
                                attributes = GetAttributes(log)
                            }).ToArray()
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(otlpLog, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private static string GetSeverityText(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "FATAL",
            _ => "INFO"
        };
    }

    private static int GetSeverityNumber(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => 1,
            LogLevel.Debug => 5,
            LogLevel.Information => 9,
            LogLevel.Warning => 13,
            LogLevel.Error => 17,
            LogLevel.Critical => 21,
            _ => 9
        };
    }

    private static object[] GetAttributes(LogEntry log)
    {
        var attributes = new List<object>();

        // Add custom attributes
        foreach (var attribute in log.Attributes)
        {
            object value = attribute.Value switch
            {
                string s => new { stringValue = s },
                int i => new { intValue = i },
                long l => new { intValue = l },
                double d => new { doubleValue = d },
                float f => new { doubleValue = (double)f },
                bool b => new { boolValue = b },
                _ => new { stringValue = JsonSerializer.Serialize(attribute.Value) }
            };

            attributes.Add(new { key = attribute.Key, value });
        }

        // Add exception attributes if present
        if (log.Exception != null)
        {
            attributes.Add(new { key = "exception.type", value = new { stringValue = log.Exception.GetType().Name } });
            attributes.Add(new { key = "exception.message", value = new { stringValue = log.Exception.Message } });
            attributes.Add(new { key = "exception.stacktrace", value = new { stringValue = log.Exception.ToString() } });
        }

        return attributes.ToArray();
    }
}