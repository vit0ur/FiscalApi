using System.Xml;
using System.Xml.Schema;
using FiscalDocumentProcessor.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace FiscalDocumentProcessor.Infrastructure.Services;

public sealed class XsdXmlValidator : IXmlValidator
{
    private readonly ILogger<XsdXmlValidator> _logger;
    private readonly XmlSchemaSet? _schemaSet;

    public XsdXmlValidator(ILogger<XsdXmlValidator> logger)
    {
        _logger = logger;
        var xsdPath = Environment.GetEnvironmentVariable("XSD_PATH");
        if (string.IsNullOrWhiteSpace(xsdPath))
        {
            _schemaSet = null;
            _logger.LogDebug("XSD_PATH not set: skipping schema validation");
            return;
        }

        try
        {
            _schemaSet = new XmlSchemaSet();
            if (File.Exists(xsdPath))
            {
                using var s = File.OpenRead(xsdPath);
                _schemaSet.Add(null, XmlReader.Create(s));
            }
            else if (Directory.Exists(xsdPath))
            {
                foreach (var f in Directory.EnumerateFiles(xsdPath, "*.xsd", SearchOption.TopDirectoryOnly))
                {
                    using var s = File.OpenRead(f);
                    _schemaSet.Add(null, XmlReader.Create(s));
                }
            }
            else
            {
                _logger.LogWarning("XSD_PATH set but path not found: {Path}", xsdPath);
                _schemaSet = null;
            }
            _schemaSet?.Compile();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load XSDs from XSD_PATH; disabling schema validation");
            _schemaSet = null;
        }
    }

    public Task ValidateAsync(byte[] xmlBytes, CancellationToken ct = default)
    {
        if (_schemaSet is null) return Task.CompletedTask;

        var errors = new List<string>();
        var settings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            ValidationType = ValidationType.Schema,
            Schemas = _schemaSet
        };
        settings.ValidationEventHandler += (s, e) => errors.Add(e.Message);

        using var ms = new MemoryStream(xmlBytes);
        using var reader = XmlReader.Create(ms, settings);
        try
        {
            while (reader.Read()) { if (ct.IsCancellationRequested) ct.ThrowIfCancellationRequested(); }
        }
        catch (XmlException xe)
        {
            throw new InvalidOperationException("Invalid XML: " + xe.Message, xe);
        }

        if (errors.Count > 0)
        {
            var msg = "XML schema validation failed: " + string.Join("; ", errors);
            throw new InvalidOperationException(msg);
        }

        return Task.CompletedTask;
    }
}
