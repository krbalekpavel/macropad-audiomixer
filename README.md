# macropad-audiomixer
This is the page about my semester project from the course EKP at CTU FEE in Prague.

The device consists of an ILI9341 TFT touchscreen display, four SSD1306 OLED displays, four linear potentiometers, four NeoPixel strips (8 LEDs each), an ADC converter ADS1115, and it is all connected to the ESP 32 S3 DevkitC development board. The touch screen is used to display 12 buttons with adjustable appearance properties and image fill options. OLED displays show the names of the applications/application group controlled by the respective fader. The ADS1115 converts the value on the faders into digital form. NeoPixel strips indicate the current volume of the controlled element obtained from the PC.

If you want to build one for yourself, don´t forget to set the display pins in the User_Setup.h file in the TFT_eSPI library directory, and follow the instructions in the readme files in the firmware and pc-app folders.


Here is the layout and schematic:
<p align="center">
  <img src="https://github.com/user-attachments/assets/6d8ed93f-2736-4937-89e6-dfc5cee5c635" />
</p>
<p align="center">
  <img src="https://github.com/user-attachments/assets/6632310a-1a97-4684-bdf4-3d168d6a7734" />
</p>

This is what a working prototype looks like:

<p align="center">
  <img src="https://github.com/user-attachments/assets/3271110a-d64f-41e0-8b50-78b023d5f44a" />
</p>

