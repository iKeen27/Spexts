namespace Spexts.Helpers;

/// <summary>
/// Smart Color-Coded Tweaking Advisor engine.
/// Returns hex color strings based on whether a setting is optimal for gaming.
/// Green = optimal, Red = sub-optimal, Yellow = needs manual check.
/// </summary>
public static class TweakAdvisor
{
    public const string Green  = "#3FB950";
    public const string Red    = "#FF5C5C";
    public const string Yellow = "#E3B341";

    // ═══════════════════════════════════════════════════════════
    // UNIFIED SECURITY TOP-4 COLOR RULES
    // ═══════════════════════════════════════════════════════════

    /// <summary>Secure Boot: Enabled → GREEN, Disabled → RED.</summary>
    public static string? ColorForSecureBoot(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/A" || value == "Unknown")
            return null;
        if (value.Contains("Enabled", StringComparison.OrdinalIgnoreCase))
            return Green;
        if (value.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
            return Red;
        return null;
    }

    /// <summary>TPM: Active → GREEN, Not Detected/Inactive → RED.</summary>
    public static string? ColorForTpm(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/A" || value == "Unknown")
            return null;
        if (value.Contains("Active", StringComparison.OrdinalIgnoreCase))
            return Green;
        if (value.Contains("Not Detected", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Inactive", StringComparison.OrdinalIgnoreCase))
            return Red;
        return null;
    }

    /// <summary>Virtualization (CPU): Enabled → GREEN, Disabled → RED.</summary>
    public static string? ColorForVirtualization(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/A" || value == "Unknown")
            return null;
        if (value.Contains("Enabled", StringComparison.OrdinalIgnoreCase))
            return Green;
        if (value.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
            return Red;
        return null;
    }

    /// <summary>
    /// Hypervisor Present: "No" → GREEN (optimal for gaming latency).
    /// "Yes" → RED (VBS/HVCI latency penalty).
    /// </summary>
    public static string? ColorForHypervisor(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/A" || value == "Unknown")
            return null;
        if (value.Equals("No", StringComparison.OrdinalIgnoreCase))
            return Green;
        if (value.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            return Red;
        return null;
    }

    // ═══════════════════════════════════════════════════════════
    // ADVANCED TWEAKING COLOR RULES
    // ═══════════════════════════════════════════════════════════

    /// <summary>VBS / Core Isolation: Disabled is better for gaming performance.</summary>
    public static string? ColorForVbs(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/A" || value == "Unknown")
            return null;
        if (value.Contains("Disabled", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Not Running", StringComparison.OrdinalIgnoreCase))
            return Green;
        if (value.Contains("Enabled", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Running", StringComparison.OrdinalIgnoreCase))
            return Red;
        return null;
    }

    /// <summary>Fast Startup: Disabled is recommended for clean boot and driver stability.</summary>
    public static string? ColorForFastStartup(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/A" || value == "Unknown")
            return null;
        if (value.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
            return Green;
        if (value.Contains("Enabled", StringComparison.OrdinalIgnoreCase))
            return Red;
        return null;
    }

    /// <summary>HPET: Disabled is recommended for lower DPC latency in gaming.</summary>
    public static string? ColorForHpet(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/A" || value == "Unknown")
            return null;
        if (value.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
            return Green;
        if (value.Contains("Enabled", StringComparison.OrdinalIgnoreCase))
            return Red;
        return null;
    }

    /// <summary>Power Plan: Ultimate/High performance is optimal for gaming.</summary>
    public static string? ColorForPowerPlan(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/A" || value == "Unknown")
            return null;
        if (value.Contains("Ultimate", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("High", StringComparison.OrdinalIgnoreCase))
            return Green;
        if (value.Contains("Balanced", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Power Saver", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Power saver", StringComparison.OrdinalIgnoreCase))
            return Red;
        return null;
    }

    /// <summary>Resizable BAR / SAM: Enabled is optimal for GPU performance.</summary>
    public static string? ColorForRebar(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "N/A" || value == "Unknown")
            return null;
        if (value.Contains("Requires Manual", StringComparison.OrdinalIgnoreCase))
            return Yellow;
        if (value.Contains("Enabled", StringComparison.OrdinalIgnoreCase))
            return Green;
        if (value.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
            return Red;
        return Yellow;
    }

    /// <summary>
    /// RAM Speed XMP hack: Analyzes if RAM is running at base speeds.
    /// DDR4 base ≤ 2666 MHz, DDR5 base (JEDEC) ≤ 4800 MHz.
    /// If at base speed → Red (XMP likely not enabled). If higher → Green.
    /// </summary>
    public static string? ColorForRamSpeed(int speedMhz)
    {
        if (speedMhz <= 0)
            return null;

        // DDR5 territory (speeds above ~3600 are likely DDR5)
        if (speedMhz > 3600)
            return speedMhz <= 4800 ? Red : Green;

        // DDR4 territory
        return speedMhz <= 2666 ? Red : Green;
    }
}
