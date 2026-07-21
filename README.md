# Nothing X for Windows

An unofficial Windows port of the **Nothing X** Android application.

## About This Project

This project brings the functionality of the official Nothing X app to Windows PCs. The original Nothing X Android APK was decompiled, reverse-engineered, and its underlying Bluetooth protocols and device communication logic were rewritten entirely in **C#**. 

This allows you to manage, customize, and view the status of your Nothing audio devices directly from your desktop, without needing an emulator or your smartphone!

## How it works
By reverse-engineering the APK, the app replicates the exact Bluetooth packets and commands the official mobile app uses to communicate with Nothing devices. It is built as a native Windows Presentation Foundation (WPF) application.

## Installation & Usage
1. **Pair your device:** First, ensure your Nothing earbuds or headphones are paired and actively connected to your Windows PC via the standard Windows Bluetooth settings.
2. **Download the app:** Go to the [Releases](../../releases) tab on the right side of this GitHub repository and download the latest `Nothing X.exe` file.
3. **Connect:** Run the executable, scan for your devices within the app, and connect to them to start managing your settings!

---
*Disclaimer: This is an unofficial, community-made project. It is not affiliated with, endorsed by, or associated with Nothing Technology Limited. All product names, logos, and brands are property of their respective owners.*
