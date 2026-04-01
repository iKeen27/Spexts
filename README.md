# Spexts

> A system diagnostic utility that analyzes PC and BIOS specifications to help apply accurate and targeted performance tweaks.

## 💡 The Problem & The Solution
In IT support and PC optimization (tweaking), there is no such thing as a "one-size-fits-all" configuration. Every PC has different hardware, different BIOS states, and unique bottlenecks. Applying generic tweaks without knowing the exact system state usually causes instability or fails to actually reduce input lag.

I needed a way to quickly and accurately scan deep Windows and BIOS settings (like VBS status, HPET, Secure Boot, and actual RAM speeds) to figure out exactly what a specific machine needs. 

**The Solution:** I built **Spexts**. It's a diagnostic scanner that reads low-level system states and instantly color-codes the results. It tells you exactly what needs fixing, allowing for immediate, targeted optimizations without the manual guesswork.

## ✨ Key Features
* **Accurate Diagnostics:** Instantly evaluates critical settings for gaming latency and system performance (VBS/Core Isolation, HPET, Fast Startup, Power Plans, and Base vs. XMP RAM Speeds).
* **Minimalist UI:** Built with WPF using a custom, tightly packed Masonry layout and a clean dark theme.
* **Portable & Self-Contained:** Packaged as a single `.exe` file. No installation required, and it includes the entire .NET 8 runtime—just plug and play.

## 🛠️ How I Built It
My core expertise is in IT hardware, system diagnostics, and PC tweaking—not traditional software engineering. To bring this tool to life, I utilized **AI Orchestration**. By engineering precise prompts, defining the system logic, and directing AI models, I successfully translated my hardware troubleshooting workflow into a functional, compiled C# application. 

## 📥 Download & Usage
You can grab the ready-to-use portable executable from the [Releases](../../releases) page.
1. Download `Spexts.exe`.
2. Right-click and select **Run as Administrator** (Admin privileges are strictly required to query low-level WMI classes and BCDedit for features like HPET and VBS).

> **⚠️ Note on Windows Defender (False Positives):**
> Because this app directly queries hardware sensors and deep OS registries, Windows Defender might flag it. To mitigate this, I have secured the application with a **Self-Signed Certificate**. However, since it lacks an expensive paid EV Digital Signature, false positives can still happen. The code is entirely open-source here for your verification, and you can safely test the compiled executable on [VirusTotal](https://www.virustotal.com/).

## 📄 License
This project is licensed under the [MIT License](LICENSE).