using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;

//using System.Threading.Tasks;
//using System.Threading;
//using System.Windows.Threading;


using Windows.System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

using Windows.Devices.Gpio;
using System.Diagnostics;




namespace BITSDevice
{
    public sealed class STOP_INFORMATION 
    {
        static int JOURNEY_PATTERN  = 1;     //Not used
        static int PROD_TIME        = 1;     //Not used
        static int CONGESTION       = 1;     //Not used
        static int GPS_LANG         = 1;     //Not used
        static int GPS_LAT          = 1;     //Not used
        static int DELAY            = 1;     //Not used
        static int BLOCK_ID         = 1;     //Not used
        static int AT_STOP          = 1;     //Not used
        static int M_START          = 1;     //Not used

        static int _TRANSACTIONID   = 1;     //used for Azure storage

        static int MAX_NOF_STOPS_1 = 9;   // (Number of stops - 1)  in this route
        static int[] mStations_Array  = { 1111, 2222, 3333, 4444, 5555, 6666, 7777, 8888, 9999, 1234};
        int m_iCurrentStationID = -1; //Current station index in the route

        static int LINE_ID = 10;        //route from Patel Nagar to Piplani
        static int BUS_ID = 610;        //ID for one of buses work on this route


        // This bus make 5 journies daily. Each Journey has an ID (journeyid) and operator operatorid
        //One Jurney will be move forward and backword
        static int MAX_NOF_JOURNIES = 5; 
        static int[,] m_JourniesArray = new int[5, 2] {{ 401, 1 }, { 402, 1}, { 403, 2}, { 404, 2}, { 405, 2}} ;
        int m_iJourneyIndex = -1; 
            

        
        static int DIRECTION_FORWARD    = 1 ;    //Forward from Piplani to Indrapuri
        static int DIRECTION_BACKWORD   = -1;    //backword from Indrapuri to Patel Nagar
        int m_iDirection = DIRECTION_FORWARD;

        int m_iNumInSensor = 0 ;   //Number of passanger move inside the bus
        int m_iNumOutSensor = 0 ;   //Number of passanger move outside the bus
        

        //Azure Data
        static DeviceClient m_deviceClient;
        static string deviceKey = "device Key here";
        //static RegistryManager registryManager;
        static string connectionString = "Conection string here"; // Refer Step 5

        public STOP_INFORMATION()
        {
            if (!string.IsNullOrEmpty(deviceKey))
            {
                m_deviceClient = DeviceClient.CreateFromConnectionString
                (connectionString, "BusDevice2", Microsoft.Azure.Devices.Client.TransportType.Http1);
            }
        }

        public void DoorOpen()
        {

            m_iNumInSensor = 0;
            m_iNumOutSensor = 0;

            m_iCurrentStationID += m_iDirection;

            if (m_iCurrentStationID >= MAX_NOF_STOPS_1)
            {
                m_iDirection = DIRECTION_BACKWORD;
            }
            else if (m_iCurrentStationID <= 0 )
            {
                m_iDirection = DIRECTION_FORWARD;
                m_iCurrentStationID = 0;
                
                m_iJourneyIndex++;
                if (m_iJourneyIndex >= MAX_NOF_JOURNIES )
                {
                    m_iJourneyIndex = 0; //error
                }
            }
            
        }

        public void PassUp()
        {
            m_iNumInSensor++;
        }

        public void PassDown()
        {
            m_iNumOutSensor++;
        }

        public void DoorClose()
        {
            SendDeviceToCloudMessagesAsync(m_deviceClient, m_iDirection, m_iJourneyIndex, m_iCurrentStationID, m_iNumInSensor, m_iNumOutSensor).Wait();

            /*
            //In case of Door closed at last stop
            if (m_iCurrentStationID >= MAX_NOF_STOPS_1)
            {
                m_iNumInSensor  = 0;  
                m_iNumOutSensor = 0;
            }*/
        }

        static async Task SendDeviceToCloudMessagesAsync(DeviceClient deviceClient , int iDirection, int iJourneyIndex, int iCurrentStationID, int iNumInSensor, int iNumOutSensor )
        {
            /*
             var deviceClient = DeviceClient.Create(iotHubUri,
                    Microsoft.Azure.Devices.Client.AuthenticationMethodFactory.
                        CreateAuthenticationWithRegistrySymmetricKey("BusDevice2", deviceKey),
                    Microsoft.Azure.Devices.Client.TransportType.Http1);
            */

            //while (true)
            //{

                var transaction = new
                {
                    id = _TRANSACTIONID++,
                    prodtime = DateTime.Now.TimeOfDay,
                    lineid = LINE_ID,
                    direction = iDirection,
                    journeypatternid = JOURNEY_PATTERN,
                    date = DateTime.Now.Date,
                    journeyid = m_JourniesArray[iJourneyIndex , 0 ],

                    operatorid = m_JourniesArray[iJourneyIndex, 1],

                    congestion = CONGESTION,
                    gpslong = GPS_LANG,
                    gpslat = GPS_LAT,
                    delay = DELAY,
                    blockid = BLOCK_ID,
                    busid = BUS_ID,

                    stationid = mStations_Array[iCurrentStationID],

                    atsop = AT_STOP,
                    m_start = M_START,

                    NumInSensor = iNumInSensor,
                    NumOutSensor = iNumOutSensor,

                    DateTimeSensor = DateTime.Now.TimeOfDay
                };

                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(transaction)));
                try { await deviceClient.SendEventAsync(message); }
                catch (Exception e) { string str = e.Message; }
            //}
        }

    }


    public sealed class StartupTask : IBackgroundTask
    {
        //Sensors Data
        private const int PASSUPLED_PIN = 17;
        private const int PASSDOWNLED_PIN = 27;
        private const int DOORCLOSEDLED_PIN = 22;

        private const int PASSUP_PIN = 5;
        private const int PASSDOWN_PIN = 6;
        private const int DOORCLOSED_PIN = 13;

        BackgroundTaskDeferral deferral;

        private GpioPin passupPin; // One passenger come inside the bus
        private GpioPin passdownPin; // One passenger left the bus
        private GpioPin doorclosedPin;// bus door closed or opened

        //LEDs used for testing the board
        private GpioPin passupledPin;
        private GpioPin passdownledPin;
        private GpioPin doorclosedledPin;
        private GpioPinValue DoorledPinValue = GpioPinValue.High;
        private GpioPinValue InledPinValue = GpioPinValue.High;
        private GpioPinValue OutledPinValue = GpioPinValue.High;
        //private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        //private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);


        private STOP_INFORMATION m_StopInformation = new STOP_INFORMATION() ; 

        public void Run(IBackgroundTaskInstance taskInstance)
        {

  

            //InitializeComponent();

            deferral = taskInstance.GetDeferral();

            InitGPIO();

            Task.Run(() =>
            {
                while (true)
                {
                    //thats right...do nothing.
                }
            });
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                //GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            passupPin = gpio.OpenPin(PASSUP_PIN);
            passupledPin = gpio.OpenPin(PASSUPLED_PIN);
            passdownPin = gpio.OpenPin(PASSDOWN_PIN);
            passdownledPin = gpio.OpenPin(PASSDOWNLED_PIN);
            doorclosedPin = gpio.OpenPin(DOORCLOSED_PIN);
            doorclosedledPin = gpio.OpenPin(DOORCLOSEDLED_PIN);

            // Initialize LED to the OFF state by first writing a HIGH value
            // We write HIGH because the LED is wired in a active LOW configuration
            passupledPin.Write(GpioPinValue.High);
            passupledPin.SetDriveMode(GpioPinDriveMode.Output);
            passdownledPin.Write(GpioPinValue.High);
            passdownledPin.SetDriveMode(GpioPinDriveMode.Output);
            doorclosedledPin.Write(GpioPinValue.High);
            doorclosedledPin.SetDriveMode(GpioPinDriveMode.Output);

            // Check if input pull-up resistors are supported
            if (passupPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                passupPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            else
                passupPin.SetDriveMode(GpioPinDriveMode.Input);

            if (passdownPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                passdownPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            else
                passdownPin.SetDriveMode(GpioPinDriveMode.Input);

            if (doorclosedPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                doorclosedPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            else
                doorclosedPin.SetDriveMode(GpioPinDriveMode.Input);

            // Set a debounce timeout to filter out switch bounce noise from a button press
            passupPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
            passdownPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
            doorclosedPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);

            // Register for the ValueChanged event so our buttonPin_ValueChanged 
            // function is called when the button is pressed
            passupPin.ValueChanged += PassUpPIN;
            passdownPin.ValueChanged += PassDownPIN;
            doorclosedPin.ValueChanged += DoorPin_ValueChanged;

            //GpioStatus.Text = "GPIO pins initialized correctly.";
        }

        private void PassUpPIN(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            // toggle the state of the LED every time the button is pressed
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                InledPinValue = (InledPinValue == GpioPinValue.Low) ? GpioPinValue.High : GpioPinValue.Low;
                passupledPin.Write(InledPinValue);

                m_StopInformation.PassUp();
             }
        }
        private void PassDownPIN(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            // toggle the state of the LED every time the button is pressed
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                OutledPinValue = (OutledPinValue == GpioPinValue.Low) ? GpioPinValue.High : GpioPinValue.Low;
                passdownledPin.Write(OutledPinValue);

                m_StopInformation.PassDown();
            }
        }

        private void DoorPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            // toggle the state of the LED every time the button is pressed
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                if (DoorledPinValue == GpioPinValue.Low)
                {
                    m_StopInformation.DoorClose();
                    DoorledPinValue = GpioPinValue.High;
                }
                else
                {
                    m_StopInformation.DoorOpen();
                    DoorledPinValue = GpioPinValue.Low;
                }
                doorclosedledPin.Write(DoorledPinValue);
            }
        }

    }
}
