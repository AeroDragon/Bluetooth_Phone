#include <SoftwareSerial.h>
//#include <string.h>

//const int TX_BT = 10;
//const int RX_BT = 11;

//const int PIN_RED = 5;
//const int PIN_GREEN = 6;
//
//SoftwareSerial btSerial(TX_BT, RX_BT);

//Process the incoming command from Windows Phone.
void processCommand(char* command) { //Takes in the command buffer. char* ch = new char; creates memory for ONE character, and assigns it to variable ch. 
Serial.println(command);        
}


  
  //if(strcmp(command,"-7.222450000") == 0) {   //strcmp : string compare. If string1 = string2 the do the following. A 0 result means there equal
    //digitalWrite(PIN_RED, HIGH);
  //}  
//}

//Send a message back to the Windows Phone.
//void sendMessage(char* message) {
//  int messageLen = strlen(message); //strlen : gets the string length. "This sentance would be result in"   32
//  if(messageLen < 256) {  
//    btSerial.write(messageLen);
//    btSerial.print(message);
//  }
//}

//Setup Arduino function
void setup() {
  Serial.begin(57600);
  Serial.println("USB Connected");
 // btSerial.begin(9600);
  
//  pinMode(PIN_RED, OUTPUT);
}

//Loop Arduino function
void loop() {
  if(Serial.available()) {
      int commandSize = (int)Serial.read(); //read first byte from serial. This should contain the size of the command. It's set on the phone.
      char command[commandSize];
      int commandPos = 0;
      while(commandPos < commandSize) {
        if(Serial.available()) {
          command[commandPos] = (char)Serial.read();  //Keep reading bluetooth data while commandPos is 0
          commandPos++;
        }
      }
      command[commandPos] = 0;
      processCommand(command);
      
//  String location = command;
//  String lattitude = location.substring(1,12);
//  String longitude = location.substring(13,12);
  
  }
  
}


