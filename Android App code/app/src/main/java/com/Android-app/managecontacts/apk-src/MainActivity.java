package com.Android-app.managecontacts.apk-src;

import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.Spinner;
import android.widget.TextView;


import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.widget.*;
import android.util.Log;
import android.view.View;
import android.view.View.OnClickListener;
import android.os.AsyncTask;


import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;



import org.apache.http.HttpEntity;
import org.apache.http.HttpResponse;
import org.apache.http.NameValuePair;
import org.apache.http.client.ClientProtocolException;
import org.apache.http.client.HttpClient;
import org.apache.http.client.ResponseHandler;
import org.apache.http.client.entity.UrlEncodedFormEntity;
import org.apache.http.client.methods.HttpGet;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.ByteArrayEntity;
import org.apache.http.impl.client.BasicResponseHandler;
import org.apache.http.impl.client.DefaultHttpClient;
import org.apache.http.message.BasicNameValuePair;
import org.apache.http.params.BasicHttpParams;
import org.apache.http.params.HttpConnectionParams;
import org.apache.http.params.HttpParams;

import org.json.JSONArray;
import org.json.JSONObject;
import org.json.JSONTokener;
import org.json.JSONException;

import com.google.gson.Gson;
import com.google.gson.JsonArray;
import com.google.gson.JsonParser;



import java.io.*;


public class MainActivity extends AppCompatActivity {

    private final int TIMEOUT_MILLISEC = 10 ;
    private String TAG = "BusTickets";


    private Button reserveTicketBtn;
    private Spinner StartStopSpin, EndStopSpin;
    private TextView arrivalTimeTxt;




    private int BusStopsIDsArray[] = {1111,2222,3333,4444,5555,6666,7777,8888,9999,1234};

    int m_iEndStopIndex, m_iStartStopIndex;
    String m_strEndStopID, m_strStartStopID, m_strAvailableBusesInfo;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);


        reserveTicketBtn = (Button) findViewById(R.id.reserveBtn);

        StartStopSpin = (Spinner) findViewById(R.id.sstopSpin);
        EndStopSpin = (Spinner) findViewById(R.id.estopSpin);

        arrivalTimeTxt = (TextView) findViewById(R.id.arrival_time_result);
        arrivalTimeTxt.setGravity(Gravity.LEFT);

        //Button Click Listener
        reserveTicketBtn.setOnClickListener(new View.OnClickListener() {
            public void onClick(View v) {
                //Read start & end stations
                m_iEndStopIndex = EndStopSpin.getSelectedItemPosition();
                m_iStartStopIndex = StartStopSpin.getSelectedItemPosition();

                m_strStartStopID = String.valueOf(BusStopsIDsArray[m_iStartStopIndex]);
                m_strEndStopID = String.valueOf(BusStopsIDsArray[m_iEndStopIndex]);


                try {
                    new Thread(new Runnable() {
                        public void run() {
                            m_strAvailableBusesInfo = "";
                            getArrivalTime();


                            runOnUiThread(new Runnable() {
                                @Override
                                public void run() {
                                    arrivalTimeTxt.setText(m_strAvailableBusesInfo);
                                    arrivalTimeTxt.refreshDrawableState();
                                }
                            });


                        }
                    }).start();
                }
                catch (Exception e)
                {
                    e.printStackTrace();
                }


            }
        });

    }

    public void getArrivalTime() {

        String strUri = "http://XYZ";

        requestContent(strUri);
    }

    public String FormatResponse(String strJson)
    {
        String strBusInformation = "Available Buses : \n" ;

        strJson = "{\"Buses\" :" + strJson + "}";


        try
        {
            JSONObject  jsonRootObject = new JSONObject(new String(strJson.getBytes("UTF-8")));

            //Get the instance of JSONArray that contains JSONObjects
            JSONArray jsonArray = jsonRootObject.optJSONArray("Buses");
            int iNofJsonArray = jsonArray.length();

            //Iterate the jsonArray and print the info of JSONObjects
            for(int i = 0 ; i < iNofJsonArray ; i++ )
            {
                JSONObject jsonObject = jsonArray.getJSONObject(i);

                String strBusID         = jsonObject.optString("BusID").toString();
                String strArrivalTime   = jsonObject.optString("ArrivalTime").toString();
                String strTravelTime    = jsonObject.optString("TravelTime").toString();
                String strAvailableSeats= jsonObject.optString("AvailableSeats").toString();

                strBusInformation += "Bus ID : "+ strBusID + "\n Arrival Time : = " + strArrivalTime  + " \n Travel Time : "+ strTravelTime + " \n Available Seats= "+ strAvailableSeats +" \n----------------------\n ";
            }
        }
        catch (java.io.UnsupportedEncodingException e)
        {
            return "";
        }
        catch (JSONException e1)
        {
            e1.printStackTrace();
        }

        return strBusInformation;
    }

    public String requestContent(String strurl)
    {
        RestClient client = new RestClient(strurl);

        client.AddParam("orig", m_strStartStopID );
        client.AddParam("dest", m_strEndStopID);

        client.AddHeader("GData-Version", "2");

        try {
            client.Execute(RequestMethod.GET);
            m_strAvailableBusesInfo = FormatResponse(client.getResponse()) ;

        } catch (Exception e) {
            e.printStackTrace();
        }

        return client.getResponse();
    }

}
