using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AtlasOf
{
    public class WebResponseResult
    {
        public bool success { get; set; }
        public string output { get; set; }
    }

    public class RequestState
    {
        // This class stores the State of the request.
        const int BUFFER_SIZE = 1024;
        public StringBuilder requestData;
        public byte[] BufferRead;
        public HttpWebRequest request;
        public HttpWebResponse response;
        public Stream streamResponse;
        public EventHandler<WebEventArgs> CallbackMethod;
        public object userState;

        public RequestState()
        {
            BufferRead = new byte[BUFFER_SIZE];
            requestData = new StringBuilder("");
            request = null;
            streamResponse = null;
            userState = null;
            Encoding = Encoding.UTF8;
        }

        public Encoding Encoding { get; set; }
    }

    public class WebEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string ResponseString { get; set; }
        public object UserState { get; set; }
        public string LastRequest { get; set; }
        //internal WaitCallback Callback;
    }

    public class SimpleWebClient
    {
        //public ManualResetEvent allDone = new ManualResetEvent(false);
        const int BUFFER_SIZE = 1024;

        public void GetRequest(string requestUrl, EventHandler<WebEventArgs> callbackMethod, object requestState)
        {
            try
            {
                HttpWebRequest myHttpWebRequest1 = HttpWebRequest.CreateHttp(requestUrl);
                //myHttpWebRequest1.Accept = "text/html, application/xhtml+xml";
                //myHttpWebRequest1.Headers["User-Agent"] = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; Trident/6.0; Touch; MASMJS";
                // Create an instance of the RequestState and assign the previous myHttpWebRequest1// object to it's request field.  
                RequestState myRequestState = new RequestState();
                myRequestState.request = myHttpWebRequest1;
                myRequestState.CallbackMethod = callbackMethod;
                myRequestState.userState = requestState;

                // Start the asynchronous request.
                IAsyncResult result = myHttpWebRequest1.BeginGetResponse(new AsyncCallback(RespCallback), myRequestState);
            }
            catch (WebException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal async Task<WebResponseResult> GetRequestAsync(string requestUrl)
        {
            try
            {
                //                GET http://neowms.sci.gsfc.nasa.gov/wms/wms?request=GetCapabilities&version=1.3.0&service=WMS HTTP/1.1
                //Accept: text/html, application/xhtml+xml, */*
                //Accept-Language: en-US,en;q=0.5
                //User-Agent: Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; Trident/6.0; Touch; MASMJS)
                //Accept-Encoding: gzip, deflate
                //Host: neowms.sci.gsfc.nasa.gov
                //DNT: 1
                //Connection: Keep-Alive

                HttpClient httpClient = new HttpClient();
                //httpClient.DefaultRequestHeaders.Date = DateTime.Now.Subtract(new TimeSpan(10, 0, 0)); 
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html, application/xhtml+xml");
                //Task<string> response = httpClient.GetStringAsync(requestUrl);
                HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                return new WebResponseResult() { output = responseBody, success = true };
            }
            catch (WebException e)
            {
                return new WebResponseResult() { output = e.Message, success = false };
            }
            catch (Exception ex)
            {
                return new WebResponseResult() { output = ex.Message, success = false };
            }
        }

        internal string GetRequestOld(string requestUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                    var response = client.GetAsync(requestUrl).Result;
                    //Task<string> responseBody = responseMsg.Result.Content.ReadAsStringAsync();
                    ////responseBody.EnsureSuccessStatusCode();
                    //responseBody.Wait();
                    //return responseBody.Result;
                    return response.Content.ReadAsStringAsync().Result;
                }
            }
            catch (WebException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal void PostRequest(string requestUrl, string postData, EventHandler<WebEventArgs> callbackMethod, object userState)
        {
            try
            {
                HttpWebRequest myHttpWebRequest1 = HttpWebRequest.CreateHttp(requestUrl);

                // Create an instance of the RequestState and assign the previous myHttpWebRequest1// object to it's request field.  
                RequestState myRequestState = new RequestState();
                myRequestState.request = myHttpWebRequest1;
                myRequestState.CallbackMethod = callbackMethod;
                myRequestState.userState = userState;
                myRequestState.requestData = new StringBuilder(postData);

                // Start the asynchronous request.
                myHttpWebRequest1.Method = "POST";
                //myHttpWebRequest1.ContentType = "application/x-www-form-urlencoded";
                //myHttpWebRequest1.Accept = "text/html, application/xhtml+xml, */*";
                //myHttpWebRequest1.Referer = "http://websig.hidrografico.pt/website/icenc/jsForm.htm";
                IAsyncResult result = myHttpWebRequest1.BeginGetRequestStream(new AsyncCallback(PostStreamCallback), myRequestState);
            }
            catch (WebException e)
            {
            }
            catch (Exception e)
            {
            }
        }


        private void PostStreamCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;

                // End the operation
                Stream postStream = myRequestState.request.EndGetRequestStream(asynchronousResult);

                // Convert the string into a byte array. 
                byte[] byteArray = Encoding.UTF8.GetBytes(myRequestState.requestData.ToString());

                // Write to the request stream.
                postStream.Write(byteArray, 0, byteArray.Length);
                myRequestState.requestData.Clear();

                // Start the asynchronous operation to get the response
                myRequestState.request.BeginGetResponse(new AsyncCallback(RespCallback), myRequestState);
            }
            catch (WebException e)
            {
                //if (args.Error is WebException)
                //{
                //    WebResponse exc = ((WebException)args.Error).Response;
                //    StreamReader reader = new StreamReader(exc.GetResponseStream());
                //    string result = reader.ReadToEnd();
                //    ((AsyncState)args.UserState).ResponseString = args.Error.Message + result;
                //}
            }
        }

        private async void RespCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest2 = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest2.EndGetResponse(asynchronousResult);

                // Read the response into a Stream object.
                Stream responseStream = myRequestState.response.GetResponseStream();
                myRequestState.streamResponse = responseStream;

                int read = await responseStream.ReadAsync(myRequestState.BufferRead, 0, BUFFER_SIZE);

                if (read > 0)
                {
                    if (myRequestState.requestData.Length == 0 && myRequestState.BufferRead[0] == 254 && myRequestState.BufferRead[1] == 255)
                    {
                        //FEFF Unicode
                        myRequestState.Encoding = new System.Text.UnicodeEncoding(true, false);
                    }
                }

                // Begin the Reading of the contents of the HTML page and print it to the console.
                //IAsyncResult asynchronousInputRead = responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), myRequestState);
                while (read > 0)// && read == BUFFER_SIZE)
                {
                    myRequestState.requestData.Append(myRequestState.Encoding.GetString(myRequestState.BufferRead, 0, read));
                    read = await responseStream.ReadAsync(myRequestState.BufferRead, 0, BUFFER_SIZE);
                }

                //, new AsyncCallback(ReadCallBack), myRequestState);
                //if (read > 0)
                //{
                //    myRequestState.requestData.Append(myRequestState.Encoding.GetString(myRequestState.BufferRead, 0, read));
                //    // IAsyncResult asynchronousResult = responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), myRequestState);
                //}

                if (myRequestState.requestData.Length > 1)
                {
                    string stringContent = myRequestState.requestData.ToString();
                    myRequestState.CallbackMethod(myRequestState, new WebEventArgs() { ResponseString = stringContent, Success = true, UserState = myRequestState.userState });
                }

                //responseStream.Close();
                //allDone.Set();
            }
            catch (WebException e)
            {
                //if (args.Error is WebException)
                //{
                //    WebResponse exc = ((WebException)args.Error).Response;
                //    StreamReader reader = new StreamReader(exc.GetResponseStream());
                //    string result = reader.ReadToEnd();
                //    ((AsyncState)args.UserState).ResponseString = args.Error.Message + result;
                //}
            }
        }

        private void ReadCallBack(IAsyncResult asyncResult)
        {
            try
            {

                RequestState myRequestState = (RequestState)asyncResult.AsyncState;
                Stream responseStream = myRequestState.streamResponse;
                int read = 0; // responseStream.EndRead(asyncResult);
                // Read the HTML page and then do something with it
                if (read > 0)
                {
                    if (myRequestState.requestData.Length == 0 && myRequestState.BufferRead[0] == 254 && myRequestState.BufferRead[1] == 255)
                    {
                        //FEFF Unicode
                        myRequestState.Encoding = new System.Text.UnicodeEncoding(true, false);
                    }

                    myRequestState.requestData.Append(myRequestState.Encoding.GetString(myRequestState.BufferRead, 0, read));
                    // IAsyncResult asynchronousResult = responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), myRequestState);
                }
                else
                {
                    if (myRequestState.requestData.Length > 1)
                    {
                        string stringContent = myRequestState.requestData.ToString();
                        myRequestState.CallbackMethod(myRequestState, new WebEventArgs() { ResponseString = stringContent, Success = true, UserState = myRequestState.userState });
                    }

                    //responseStream.Close();
                    //allDone.Set();
                }

            }
            catch (WebException e)
            {
                // Need to handle the exception
            }
        }
    }
}
