# DragonPedal
Allows serial and many USB foot pedals to control Nuance Dragon Mic 

Typically serial footpedals simply act as joysticks and if your footpedal is this type than all buttons are simply mapped to simulate the plus character on the keyboard being pressed as that is the default button for Dragon Mics to toggle start and stop.

If your foot pedal is of type USB, than they are harder to specifically target because different models have different names and just show up in hardware as a generic usb input device. This app targets USB devices that contain the word pedal somewhere in their name (which luckily many do.) If this is found than once again all buttons will be mapped to toggle the Dragon Mic.

Lastly their is a timer on clicks so that multiple clicks cannot occur at once.

The application will run minimized as a icon in the system notification tray and upon double click will open up a GUI where you can exit the app or just minimize again.
