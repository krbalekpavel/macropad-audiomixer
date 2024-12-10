#include <Wire.h>
#include <Arduino.h>
#include <U8g2lib.h>
#include <Adafruit_NeoPixel.h>
#include <ADS1X15.h>
#include "FS.h"
#include <TFT_eSPI.h> 
#include <PNGdec.h>
#include <XPT2046_Touchscreen.h>
#include <WiFi.h>
#include <WebSocketsServer.h>
#include <ArduinoJson.h>
#include <HTTPClient.h>
#include "base64.hpp"
#include "sipka-r.h"  //static image
#include "sipka-l.h"  //static image


#define USE_LINE_BUFFER  // Enable for faster rendering

// Pin definitions
#define CTFT_CS    7
#define CTFT_RST   17
#define CTFT_DC    18
#define CTOUCH_CS  10

#define ADC_SDA 8
#define ADC_SCL 9
#define OLED1_SDA 6
#define OLED1_SCL 5
#define OLED2_SDA 2
#define OLED2_SCL 1
#define OLED3_SDA 41
#define OLED3_SCL 42
#define OLED4_SDA 39
#define OLED4_SCL 40

#define LED_STRIP1_PIN 38
#define LED_STRIP2_PIN 37
#define LED_STRIP3_PIN 36
#define LED_STRIP4_PIN 35

#define NUM_LEDS 10

#define OSCREEN_WIDTH 128
#define OSCREEN_HEIGHT 32
#define CALIBRATION_FILE "/TouchCalData1"
// Set REPEAT_CAL to true instead of false to run calibration
// again, otherwise it will only be done once.
// Repeat calibration if you change the screen rotation.
#define REPEAT_CAL false

uint32_t colors[] = {0xFFFFFF,0xFFFFFF,0xFFFFFF,0xFFFFFF};

// WiFi credentials
const char* ssid = "ssid";
const char* password = "password";

// WebSocket server on port 81
WebSocketsServer webSocket = WebSocketsServer(81);


// Initialize OLED displays on different I2C buses (U8G2_R0, /* clock=*/ 19, /* data=*/ 18, /* reset=*/ U8X8_PIN_NONE);
U8G2_SSD1306_128X64_NONAME_F_SW_I2C display1(U8G2_R0, OLED1_SCL, OLED1_SDA, U8X8_PIN_NONE);
U8G2_SSD1306_128X64_NONAME_F_SW_I2C display2(U8G2_R0, OLED2_SCL, OLED2_SDA, U8X8_PIN_NONE);
U8G2_SSD1306_128X64_NONAME_F_SW_I2C display3(U8G2_R0, OLED3_SCL, OLED3_SDA, U8X8_PIN_NONE);
U8G2_SSD1306_128X64_NONAME_F_SW_I2C display4(U8G2_R0, OLED4_SCL, OLED4_SDA, U8X8_PIN_NONE);


// Initialize ADS1115 objects on different I2C buses
ADS1115 ADS(0x48, &Wire);

// TFT screen and touchscreen
TFT_eSPI tft = TFT_eSPI();

#include "support_functions.h"
#define MAX_IMAGE_WIDTH 240 // Sets rendering line buffer lengths
PNG png; 

// LED strips
Adafruit_NeoPixel strip1 = Adafruit_NeoPixel(NUM_LEDS, LED_STRIP1_PIN, NEO_GRB + NEO_KHZ800);
Adafruit_NeoPixel strip2 = Adafruit_NeoPixel(NUM_LEDS, LED_STRIP2_PIN, NEO_GRB + NEO_KHZ800);
Adafruit_NeoPixel strip3 = Adafruit_NeoPixel(NUM_LEDS, LED_STRIP3_PIN, NEO_GRB + NEO_KHZ800);
Adafruit_NeoPixel strip4 = Adafruit_NeoPixel(NUM_LEDS, LED_STRIP4_PIN, NEO_GRB + NEO_KHZ800);



int currentPage = 0;
const int numPages = 4; // Total number of pages
const int numButtonsPerPage = 12; // Number of buttons per page
const int buttonWidth = 73; // Width of each button
const int buttonHeight = 73; // Height of each button
const int margin = 8; // Margin between buttons
const int textmargin = 1; // Margin between buttons
const int textsize = 2; // Margin between buttons
const int buttonsPerRow = 3; // Number of buttons per row
// Assume these are the maximum and minimum possible ADC values.
const int16_t ADC_MIN = 0;
const int16_t ADC_MAX = 16500; 

// Variables to store the last known values
int lastFader1Percentage = -1;
int lastFader2Percentage = -1;
int lastFader3Percentage = -1;
int lastFader4Percentage = -1;

int neoBrightness = 3;

struct SplitResult {
    char* lines[4];
    int count;
};

struct Button {
  String text;
  uint16_t bgColor;
  uint16_t textColor;
  uint16_t outlineColor;
  String imageURL;
  String imageData;
};

//Array of all the buttons
Button buttons[4][12] = {
  {
    {"Button", 0x1111, 0xFFFF, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "https://external-content.duckduckgo.com/iu/?u=https%3A%2F%2Ficons.iconarchive.com%2Ficons%2Fsteve%2Fzondicons%2F72%2FCheckmark-Outline-icon.png&f=1&nofb=1&ipt=e586384816660a79f86ce50f7c06e0ddcf5a99adb203db1a687ac94411932a42&ipo=images", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"=>", 0x0000, 0xFFFF, 0x07FF, "r", ""}
  },
  {
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"<=", 0x0000, 0xFFFF, 0x07FF, "l", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"=>", 0x0000, 0xFFFF, 0x07FF, "r", ""}
  },
  {
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"<=", 0x0000, 0xFFFF, 0x07FF, "l", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"=>", 0x0000, 0xFFFF, 0x07FF, "r", ""}
  },
  {
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"<=", 0x0000, 0xFFFF, 0x07FF, "l", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""},
    {"Button", 0xFFFF, 0x0000, 0x07E0, "", ""}
  }
};
int16_t xpos = 0;
int16_t ypos = 0;

void drawButton(int page, int buttonIndex, int x, int y) {
  Button button = buttons[page][buttonIndex];

  if (button.imageURL.length() > 0) {
    tft.fillRoundRect(x, y, buttonWidth, buttonHeight,8, button.bgColor);
    tft.drawRoundRect(x, y, buttonWidth, buttonHeight,8, button.outlineColor);
    tft.drawRoundRect(x-1, y-1, buttonWidth+2, buttonHeight+2,8, button.outlineColor);
    if(button.imageURL.length() == 1)
    {
      xpos = x;
      ypos = y;
      if(button.imageURL == "r")
      {
        int16_t r = png.openFLASH((uint8_t *)sipka_r, sizeof(sipka_r), FLpngDraw);
        if (r == PNG_SUCCESS) {
          tft.startWrite();
          r = png.decode(NULL, 0);
          tft.endWrite();
        }

      } else if (button.imageURL == "l") {
        int16_t l = png.openFLASH((uint8_t *)sipka_l, sizeof(sipka_l), FLpngDraw);
        if (l == PNG_SUCCESS) {
          tft.startWrite();
          l = png.decode(NULL, 0);
          tft.endWrite();
        }

      }
    } else {
      setPngPosition(x, y);
      load_png(button.imageURL.c_str());

    }
  } else {
    // Draw button background and text
    tft.fillRoundRect(x, y, buttonWidth, buttonHeight,8, button.bgColor);
    tft.drawRoundRect(x, y, buttonWidth, buttonHeight,8, button.outlineColor);
    tft.drawRoundRect(x-1, y-1, buttonWidth+2, buttonHeight+2,8, button.outlineColor);
    tft.setCursor(x + textmargin, y + (buttonHeight / 2) - 10);
    tft.setTextColor(button.textColor);
    tft.setTextSize(textsize);
    tft.print(button.text);
  }
}
void drawButtonOutline(int page, int buttonIndex, int x, int y) {
  Button button = buttons[page][buttonIndex];
  tft.drawRoundRect(x, y, buttonWidth, buttonHeight,8, button.outlineColor);
  tft.drawRoundRect(x-1, y-1, buttonWidth+2, buttonHeight+2,8, button.outlineColor);
}

void setup() {
  Serial.begin(115200);

  // WiFi setup
   Serial.println(WiFi.begin(ssid, password));
  while (WiFi.status() != WL_CONNECTED) {
    delay(50);
    Serial.print(".");
  }
  Serial.println("WiFi connected");
  Serial.println(WiFi.localIP());

  // WebSocket setup
  webSocket.onEvent(webSocketEvent);
  webSocket.begin();
  
  Serial.println("WebSocket Start");
  delay(500);

  // TFT and touchscreen setup
  tft.init();
   // Set the rotation before we calibrate
  tft.setRotation(0);
   // Calibrate the touch screen and retrieve the scaling factors
  Serial.println(TFT_MISO);
  Serial.println(TFT_MOSI);
  Serial.println(TFT_SCLK);
  Serial.println(TFT_CS);
  Serial.println(TFT_DC);
  Serial.println(TFT_RST);
  Serial.println(TOUCH_CS);

  tft.fillScreen(TFT_GREEN);
  delay(1000);
  touch_calibrate();
   // Clear the screen
  tft.fillScreen(TFT_BLACK);
   // Draw keypad background
  tft.fillRect(0, 0, 240, 320, TFT_DARKGREY);


  delay(500);

  // ADC setup
  Wire.begin();
  

  delay(500);
  Serial.println(ADS.begin());
  ADS.setGain(0);      
  ADS.setDataRate(7);  //  0 = slow   4 = medium   7 = fast
  ADS.setMode(0);      //  continuous mode
  Serial.println(ADS.readADC(0));      //  first read to trigger

  delay(500);


  //OLED init

  Serial.println(display1.begin());
  Serial.println(display2.begin());
  Serial.println(display3.begin());
  Serial.println(display4.begin());

  display1.clearBuffer();
  display1.setFont(u8g2_font_logisoso32_tf);
  display1.drawStr(0,32,"11111111");
  display1.sendBuffer();	 
  display2.clearBuffer();
  display2.setFont(u8g2_font_logisoso32_tf);
  display2.drawStr(0,32,"222222222");
  display2.sendBuffer();	 
  display3.clearBuffer();
  display3.setFont(u8g2_font_logisoso32_tf);
  display3.drawStr(0,32,"333333333");
  display3.sendBuffer();	 
  display4.clearBuffer();
  display4.setFont(u8g2_font_logisoso32_tf);
  display4.drawStr(0,32,"4444444444");
  display4.sendBuffer();	 
  
  // LED strip setup
  strip1.begin();
  strip2.begin();
  strip3.begin();
  strip4.begin();

  setLEDBrightness(neoBrightness);

  strip1.show();
  strip2.show();
  strip3.show();
  strip4.show();
  drawAllButtons();
}

void touch_calibrate() {
 uint16_t calData[5];
 uint8_t calDataOK = 0;

 // check file system exists
 if (!SPIFFS.begin()) {
  Serial.println("spiffs error");
  SPIFFS.format();
  Serial.println(SPIFFS.begin());
 }

 // check if calibration file exists and size is correct
 if (SPIFFS.exists(CALIBRATION_FILE)) {
  if (REPEAT_CAL)
  {
    // Delete if we want to re-calibrate
    SPIFFS.remove(CALIBRATION_FILE);
  }
  else
  {
    File f = SPIFFS.open(CALIBRATION_FILE, "r");
    if (f) {
      if (f.readBytes((char *)calData, 14) == 14)
        calDataOK = 1;
      f.close();
    }
  }
 }
 if (calDataOK && !REPEAT_CAL) {
  // calibration data valid
  tft.setTouch(calData);
 } else {
  // data not valid so recalibrate
  tft.fillScreen(TFT_BLACK);
  tft.setCursor(20, 0);
  tft.setTextFont(2);
  tft.setTextSize(1);
  tft.setTextColor(TFT_WHITE, TFT_BLACK);

  tft.println("Touch corners as indicated");

  tft.setTextFont(1);
  tft.println();
  if (REPEAT_CAL) {
    tft.setTextColor(TFT_RED, TFT_BLACK);
    tft.println("Set REPEAT_CAL to false to stop this running again!");
  }

  tft.calibrateTouch(calData, TFT_MAGENTA, TFT_BLACK, 15);

  tft.setTextColor(TFT_GREEN, TFT_BLACK);
  tft.println("Calibration complete!");
  // store data
  File f = SPIFFS.open(CALIBRATION_FILE, "w");
  if (f) {
    f.write((const unsigned char *)calData, 14);
    f.close();
  }
 }
}

//Handle incoming requests from PC
void webSocketEvent(uint8_t num, WStype_t type, uint8_t * payload, size_t length) {
  if (type == WStype_TEXT) {
    JsonDocument doc; 
    deserializeJson(doc, payload);

    String command = doc["command"];
    
    if (command == "setButton") {
      int page = doc["page"];
      int button = doc["button"];
      if((page == 0 && button == 11) || (page == 1 && button == 9) || (page == 1 && button == 11) ||(page == 2 && button == 9) ||(page == 2 && button == 11)||(page == 3 && button == 9))
      { 

      } else {     
         buttons[page][button].text = doc["text"].as<String>();
         buttons[page][button].bgColor = doc["bgColor"];
         buttons[page][button].textColor = doc["textColor"];
         buttons[page][button].outlineColor = doc["outlineColor"];
         buttons[page][button].imageURL = doc["imageURL"].as<String>(); // Image URL
         buttons[page][button].imageData = ""; // Clear image data
         if(page == currentPage)
         {
          drawButtonFromIndex(button);
         }
      }
      
    } else if (command == "setButtonImage") {
      int page = doc["page"];
      int button = doc["button"];
      String imageData = doc["imageData"].as<String>(); // Base64 encoded image data
      buttons[page][button].imageData = imageData;
      buttons[page][button].imageURL = ""; // Clear image URL
      drawAllButtons();
    } else if (command == "setOLEDText") {
      int oled = doc["oled"];
      String text = doc["text"];
      setOLEDText(oled, text);
    } else if (command == "setLEDColor") {
      int strip = doc["strip"];
      uint32_t color = doc["color"];
      colors[strip-1]=color;
      //setLEDStripColor(strip, color);
    } else if (command == "setBrightness") {
      int brightness = doc["brightness"];
      setLEDBrightness(brightness);
    }
  }
}

SplitResult splitStringByNewline(const char *input) {
    SplitResult result;
    result.count = 0;
    
    const char *start = input;
    const char *end;

    // Initialize the lines array to null pointers
    for (int i = 0; i < 4; i++) {
        result.lines[i] = nullptr;
    }

    // Loop through the string and split by newline characters
    while (*start && result.count < 4) {
        end = strchr(start, '\n');

        if (end == nullptr) {
            end = start + strlen(start); // Point to the end of the string
        }

        int length = end - start;
        result.lines[result.count] = (char*)malloc(length + 1);
        
        if (result.lines[result.count] != nullptr) {
            strncpy(result.lines[result.count], start, length);
            result.lines[result.count][length] = '\0'; // Null-terminate the string
            result.count++;
        }

        if (*end == '\0') {
            break;
        }

        start = end + 1;
    }

    // Fill remaining lines with empty strings if there are fewer than 4 lines
    for (int i = result.count; i < 4; i++) {
        result.lines[i] = (char*)malloc(1);
        if (result.lines[i] != nullptr) {
            result.lines[i][0] = '\0';
        }
    }

    return result;
}


void setOLEDText(int id, String text)
{
  U8G2_SSD1306_128X64_NONAME_F_SW_I2C oled = display1;
  switch (id){
    case 1: oled = display1; break;
    case 2: oled = display2; break;
    case 3: oled = display3; break;
    case 4: oled = display4; break;
  }
  SplitResult res = splitStringByNewline(text.c_str());
  setDisplay(oled, res.count, res.lines);
}


void setDisplay(U8G2_SSD1306_128X64_NONAME_F_SW_I2C oled, int lineCount, char *text[4]) {
    int offsets[4];
    const uint8_t* font;
    //Dynamic size based on the number of lines of text
    if(lineCount == 1){
      offsets[0] = 32;
      font = u8g2_font_logisoso32_tf;
    }
    if(lineCount == 2){
      offsets[0] = 16;
      offsets[1] = 32;
      font = u8g2_font_logisoso16_tf;
    }
    if(lineCount == 3){
      offsets[0] = 9;
      offsets[1] = 20;
      offsets[2] = 30;
      font = u8g2_font_profont12_tf;
    }
    if(lineCount == 4){
      offsets[0] = 8;
      offsets[1] = 16;
      offsets[2] = 24;
      offsets[3] = 32;
      font = u8g2_font_t0_11_tf;
    }
  if (lineCount < 5) {
    oled.clearBuffer();
    oled.setFont(font);
    if (lineCount > 0) {
      oled.drawStr(0, offsets[0], text[0]);
      if (lineCount > 1) {
        oled.drawStr(0, offsets[1], text[1]);
        if (lineCount > 2) {
          oled.drawStr(0, offsets[2], text[2]);
          if (lineCount > 3) {
            oled.drawStr(0, offsets[3], text[3]);
          }
        }
      }
    }
    oled.sendBuffer();	
  }
}

void setLEDStripColor(int strip, uint32_t color) {
  Adafruit_NeoPixel* selectedStrip;
  switch (strip) {
    case 1: selectedStrip = &strip1; break;
    case 2: selectedStrip = &strip2; break;
    case 3: selectedStrip = &strip3; break;
    case 4: selectedStrip = &strip4; break;
  }
  for (int i = 0; i < NUM_LEDS; i++) {
    selectedStrip->setPixelColor(i, color);
  }
  selectedStrip->show();
}

void setLEDBrightness(int brightness) {
  neoBrightness = brightness;
  strip1.setBrightness(brightness);
  strip2.setBrightness(brightness);
  strip3.setBrightness(brightness);
  strip4.setBrightness(brightness);

  strip1.show();
  strip2.show();
  strip3.show();
  strip4.show();
}

void readFaders() {
    // Read raw ADC values
    int16_t fader1Value = ADS.readADC(0); 
    int16_t fader2Value = ADS.readADC(1); 
    int16_t fader3Value = ADS.readADC(2); 
    int16_t fader4Value = ADS.readADC(3); 

    // Convert to percentage (range 0-100)
    int fader1Percentage = map(fader1Value, ADC_MIN, ADC_MAX, 0, 100);
    int fader2Percentage = map(fader2Value, ADC_MIN, ADC_MAX, 0, 100);
    int fader3Percentage = map(fader3Value, ADC_MIN, ADC_MAX, 0, 100);
    int fader4Percentage = map(fader4Value, ADC_MIN, ADC_MAX, 0, 100);

    //Limit the range of the values
    if(fader1Percentage < 3)
    {
      fader1Percentage = 0;
    }
    if(fader1Percentage > 97)
    {
      fader1Percentage = 100;
    }
    if(fader2Percentage > 100)
    {
      fader2Percentage = 100;
    }
    if(fader3Percentage > 100)
    {
      fader3Percentage = 100;
    }
    if(fader4Percentage > 100)
    {
      fader4Percentage = 100;
    }

    // Update LED strips with the original ADC values
    updateLEDStrip(fader1Percentage, &strip1,0);
    updateLEDStrip(fader2Percentage, &strip2,1);
    updateLEDStrip(fader3Percentage, &strip3,2);
    updateLEDStrip(fader4Percentage, &strip4,3);

    // Send data only if there's a change in value
    if (fader1Percentage != lastFader1Percentage) {
        sendFaderData(1, fader1Percentage);
        strip1.setPixelColor(0, strip1.Color(255, 255, 0));
        strip1.show();
        lastFader1Percentage = fader1Percentage;
        delay(5);
    }
    if (fader2Percentage != lastFader2Percentage) {
        sendFaderData(2, fader2Percentage);
        strip2.setPixelColor(0, strip2.Color(255, 255, 0));
        strip2.show();
        lastFader2Percentage = fader2Percentage;
        delay(5);
    }
    if (fader3Percentage != lastFader3Percentage) {
        sendFaderData(3, fader3Percentage);
        strip3.setPixelColor(0, strip3.Color(255, 255, 0));
        strip3.show();
        lastFader3Percentage = fader3Percentage;
        delay(5);
    }
    if (fader4Percentage != lastFader4Percentage) {
        sendFaderData(4, fader4Percentage);
        strip4.setPixelColor(0, strip4.Color(255, 255, 0));
        strip4.show();
        lastFader4Percentage = fader4Percentage;
        delay(5);
    }
}

void updateLEDStrip(int16_t value, Adafruit_NeoPixel* strip, int id) {
  int numLedsOn = map(value, 0, 100, 0, NUM_LEDS);
  for (int i = 0; i < NUM_LEDS; i++) {
    if (i < numLedsOn) {
      strip->setPixelColor(i, colors[id]);
    } else {
      strip->setPixelColor(i, strip->Color(0, 0, 0));
    }
  }
  strip->show();
}

void handleTouch() {
  uint16_t t_x = 9999, t_y = 9999; // To store the touch coordinates
  bool pressed = tft.getTouch(&t_x, &t_y);

  if (!pressed) return; // No touch detected, exit the function

  // Calculate the button grid location
  int buttonIndex = -1;
  int x, y;

  for (int row = 0; row < numButtonsPerPage / buttonsPerRow; row++) {
    for (int col = 0; col < buttonsPerRow; col++) {
      // Calculate button coordinates
      x = col * (buttonWidth + margin);
      y = row * (buttonHeight + margin);

      // Check if touch coordinates are within the bounds of the button
      if (t_x >= x && t_x <= x + buttonWidth && t_y >= y && t_y <= y + buttonHeight) {
        buttonIndex = row * buttonsPerRow + col;
        break;
      }
    }
    if (buttonIndex != -1) break;
  }

  if (buttonIndex != -1 && buttonIndex < numButtonsPerPage) {
    uint16_t prevOutline = buttons[currentPage][buttonIndex].outlineColor;
    buttons[currentPage][buttonIndex].outlineColor = 0x915C;
    drawButtonOutlineFromIndex(buttonIndex);
    callButtonFunction(currentPage, buttonIndex);
    delay(500);
    buttons[currentPage][buttonIndex].outlineColor = prevOutline;
    drawButtonOutlineFromIndex(buttonIndex);
  }
}

void drawAllButtons() {
  int x, y;
  int buttonIndex = 0;
  tft.fillScreen(TFT_BLACK);
  for (int row = 0; row < numButtonsPerPage / buttonsPerRow; row++) {
    for (int col = 0; col < buttonsPerRow; col++) {
      if (buttonIndex >= numButtonsPerPage) return; // Exit if no more buttons to draw

      x = col * (buttonWidth + margin);
      y = row * (buttonHeight + margin);
      drawButton(currentPage, buttonIndex, x, y);
      buttonIndex++;
    }
  }
}

void drawButtonFromIndex(int targetButtonIndex) {
  int x, y;
  int buttonIndex = 0;
  
  for (int row = 0; row < numButtonsPerPage / buttonsPerRow; row++) {
    for (int col = 0; col < buttonsPerRow; col++) {
      if (buttonIndex >= numButtonsPerPage) return; // Exit if no more buttons to draw
      if(buttonIndex == targetButtonIndex)
      {
       x = col * (buttonWidth + margin);
       y = row * (buttonHeight + margin);
       drawButton(currentPage, buttonIndex, x, y);
       return;
      }
      buttonIndex++;
    }
  }
}

//Enables to change border color for each button
void drawButtonOutlineFromIndex(int targetButtonIndex) {
  int x, y;
  int buttonIndex = 0;
  
  for (int row = 0; row < numButtonsPerPage / buttonsPerRow; row++) {
    for (int col = 0; col < buttonsPerRow; col++) {
      if (buttonIndex >= numButtonsPerPage) return; // Exit if no more buttons to draw
      if(buttonIndex == targetButtonIndex)
      {
       x = col * (buttonWidth + margin);
       y = row * (buttonHeight + margin);
       drawButtonOutline(currentPage, buttonIndex, x, y);
       return;
      }
      buttonIndex++;
    }
  }
}

//Sends info about a buttonpress and updates TFT
void callButtonFunction(int page, int index) {
  if((page == 0 && index == 11) || (page == 1 && index == 9) || (page == 1 && index == 11) ||(page == 2 && index == 9) ||(page == 2 && index == 11)||(page == 3 && index == 9))
      {
        if(index == 9)
        {
          currentPage = currentPage - 1;
          drawAllButtons();
        }
        if(index == 11)
        {
          currentPage = currentPage + 1;
          drawAllButtons();
        }
      } else {
        JsonDocument doc;
        doc["command"] = "button";
        doc["button"] = index;
        doc["page"] = page;

        String output;
        serializeJson(doc, output);

        webSocket.broadcastTXT(output); 
      } 
}

void sendFaderData(int fader, int16_t value) {
  JsonDocument doc;
  doc["command"] = "fader";
  doc["fader"] = fader;
  doc["value"] = value;

  String output;
  serializeJson(doc, output);

  webSocket.broadcastTXT(output);
}
void drawBitmap(int x, int y, const char *imageData) {
  int bmpWidth, bmpHeight;
  uint8_t bmpDepth;
  uint32_t bmpImageoffset;
  uint32_t rowSize;
  uint8_t sdbuffer[3 * 20]; // 20 pixels per buffer
  uint8_t buffidx = sizeof(sdbuffer);
  const uint8_t *dataPtr = reinterpret_cast<const uint8_t*>(imageData);

  if ((x >= tft.width()) || (y >= tft.height())) return;

  // Read BMP header from the provided data
  if (read16(dataPtr) == 0x4D42) {
    read32(dataPtr); // File size
    read32(dataPtr); // Creator bytes
    bmpImageoffset = read32(dataPtr); // Start of image data
    read32(dataPtr); // Header size
    bmpWidth  = read32(dataPtr);
    bmpHeight = read32(dataPtr);
    bmpDepth  = read16(dataPtr); // Bits per pixel

    if ((bmpDepth == 24) && (read16(dataPtr) == 0)) {
      y += bmpHeight - 1;
      bool goodBmp = true;
      rowSize = (bmpWidth * 3 + 3) & ~3;
      uint32_t rowOffset = bmpImageoffset + (bmpHeight - 1) * rowSize;
      
      for (int row = 0; row < bmpHeight; row++) {
        if (buffidx >= sizeof(sdbuffer)) {
          memcpy(sdbuffer, dataPtr + rowOffset - (sizeof(sdbuffer)), sizeof(sdbuffer));
          buffidx = 0;
        }

        for (int col = 0; col < bmpWidth; col++) {
          tft.drawPixel(x + col, y - row, tft.color565(sdbuffer[buffidx + 2], sdbuffer[buffidx + 1], sdbuffer[buffidx]));
          buffidx += 3;
        }

        if (buffidx >= sizeof(sdbuffer)) {
          memcpy(sdbuffer, dataPtr + rowOffset - (sizeof(sdbuffer)), sizeof(sdbuffer));
          buffidx = 0;
        }
        
        // Update rowOffset for the next row
        rowOffset -= rowSize;
      }
    }
  }
}

void FLpngDraw(PNGDRAW *pDraw) {
  uint16_t lineBuffer[MAX_IMAGE_WIDTH];          // Line buffer for rendering
  uint8_t  maskBuffer[1 + MAX_IMAGE_WIDTH / 8];  // Mask buffer

  png.getLineAsRGB565(pDraw, lineBuffer, PNG_RGB565_BIG_ENDIAN, 0xffffffff);

  if (png.getAlphaMask(pDraw, maskBuffer, 255)) {
    // Note: pushMaskedImage is for pushing to the TFT and will not work pushing into a sprite
    tft.pushMaskedImage(xpos, ypos + pDraw->y, pDraw->iWidth, 1, lineBuffer, maskBuffer);
  }
}

uint16_t read16(const uint8_t *&dataPtr) {
  uint16_t result;
  result = dataPtr[0] | (dataPtr[1] << 8);
  dataPtr += 2; // Move the pointer forward
  return result;
}

uint32_t read32(const uint8_t *&dataPtr) {
  uint32_t result;
  result = dataPtr[0] | (dataPtr[1] << 8) | (dataPtr[2] << 16) | (dataPtr[3] << 24);
  dataPtr += 4; // Move the pointer forward
  return result;
}


void loop() {
  webSocket.loop();
  readFaders();
  handleTouch(); // Check for touch input to switch pages
}
  // Draw buttons for the current page
  


