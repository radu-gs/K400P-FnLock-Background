# K400+ Fn Lock

An app that keeps your [K400+ wireless keyboard](https://www.logitech.com/en-us/products/keyboards/k400-plus-touchpad-keyboard.920-007119.html) in its Fn Lock mode at all times without needing to install Logi Options.

## Why?

- There is no dedicated Fn Lock key on the K400+.
- The less said about Logi Options, the better.
- The Fn Lock setting is reset to disabled whenever the keyboard loses signal to the receiver.

## How?

As Logitech's Unifying Receiver remains connected at all times, automatic device detection can be difficult.

Using [busdog](https://github.com/djpnewton/busdog) I found that one of the devices installed by the Unifying Receiver sends a specific HID report on reconnect. 

If the receiver is present, the app listens for the report, then sends the Fn Lock command to the keyboard.

## Download

Get the latest package from the Releases section.

## Usage

The app lives entirely in the notification tray. Right click the app icon to interact with it.

## Dependencies

 - .NET 8.0 
 - [HidApi.Net](https://github.com/badcel/HidApi.Net)

## Building

    dotnet build K400P-FnLock-Background.sln

## Other remarks

[HIDAPI](https://github.com/libusb/hidapi) binaries are included in the project in order for [HidApi.Net](https://github.com/badcel/HidApi.Net) to function and for user convenience. See license-hidapi.txt for more information.

Inspired by [h8man/k400p-fn-lock-win](https://github.com/h8man/k400p-fn-lock-win).
