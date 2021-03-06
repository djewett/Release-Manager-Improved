﻿<?xml version="1.0"?>
<configuration>
  <appSettings>
    <add key="coreServiceEndpoint" value="wsHttp"/>
  </appSettings>
  
    <system.web>
      <webServices>
        <protocols>
          <add name="HttpPost"/>
          <add name="HttpGet"/>
        </protocols>
      </webServices>
      <compilation debug="true" targetFramework="4.0" />
      <httpRuntime requestValidationMode="2.0" />
    </system.web>

  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding name="basicHttp" closeTimeout="00:01:00" openTimeout="00:01:00"
          receiveTimeout="00:10:00" sendTimeout="00:01:00" allowCookies="false"
          bypassProxyOnLocal="false" hostNameComparisonMode="StrongWildcard"
          maxBufferSize="5000000" maxBufferPoolSize="524288" maxReceivedMessageSize="5000000"
          messageEncoding="Text" textEncoding="utf-8" transferMode="Buffered"
          useDefaultWebProxy="true">
          <readerQuotas maxDepth="32" maxStringContentLength="10485760" maxArrayLength="16384"
            maxBytesPerRead="4096" maxNameTableCharCount="16384" />
          <security mode="TransportCredentialOnly">
            <transport clientCredentialType="None" proxyCredentialType="None"
              realm="" />
            <message clientCredentialType="UserName" algorithmSuite="Default" />
          </security>
        </binding>
        <binding name="streamDownload_basicHttp" closeTimeout="00:01:00"
          openTimeout="00:01:00" receiveTimeout="00:10:00" sendTimeout="00:01:00"
          allowCookies="false" bypassProxyOnLocal="false" hostNameComparisonMode="StrongWildcard"
          maxBufferSize="5000000" maxBufferPoolSize="524288" maxReceivedMessageSize="5000000"
          messageEncoding="Mtom" textEncoding="utf-8" transferMode="Buffered"
          useDefaultWebProxy="true">
          <readerQuotas maxDepth="32" maxStringContentLength="10485760" maxArrayLength="16384"
            maxBytesPerRead="4096" maxNameTableCharCount="16384" />
          <security mode="TransportCredentialOnly">
            <transport clientCredentialType="Windows" proxyCredentialType="None"
              realm="" />
            <message clientCredentialType="UserName" algorithmSuite="Default" />
          </security>
        </binding>
        <binding name="streamUpload_basicHttp" closeTimeout="00:01:00"
          openTimeout="00:01:00" receiveTimeout="00:10:00" sendTimeout="00:01:00"
          allowCookies="false" bypassProxyOnLocal="false" hostNameComparisonMode="StrongWildcard"
          maxBufferSize="5000000" maxBufferPoolSize="524288" maxReceivedMessageSize="5000000"
          messageEncoding="Mtom" textEncoding="utf-8" transferMode="Buffered"
          useDefaultWebProxy="true">
          <readerQuotas maxDepth="32" maxStringContentLength="10485760" maxArrayLength="16384"
            maxBytesPerRead="4096" maxNameTableCharCount="16384" />
          <security mode="None">
            <transport clientCredentialType="None" proxyCredentialType="None"
              realm="" />
            <message clientCredentialType="UserName" algorithmSuite="Default" />
          </security>
        </binding>
      </basicHttpBinding>
      <wsHttpBinding>
        <binding name="wsHttp" closeTimeout="00:01:00" openTimeout="00:01:00"
          receiveTimeout="00:10:00" sendTimeout="00:01:00" bypassProxyOnLocal="false"
          transactionFlow="true" hostNameComparisonMode="StrongWildcard"
          maxBufferPoolSize="524288" maxReceivedMessageSize="5000000" messageEncoding="Text"
          textEncoding="utf-8" useDefaultWebProxy="true" allowCookies="false">
          <readerQuotas maxDepth="32" maxStringContentLength="10485760" maxArrayLength="16384"
            maxBytesPerRead="4096" maxNameTableCharCount="16384" />
          <reliableSession ordered="true" inactivityTimeout="00:10:00"
            enabled="false" />
          <security mode="Message">
            <transport clientCredentialType="Windows" proxyCredentialType="None"
              realm="" />
            <message clientCredentialType="Windows" negotiateServiceCredential="true"
              algorithmSuite="Default" />
          </security>
        </binding>
      </wsHttpBinding>
    </bindings>
    <client>
      <endpoint address="http://vmrecmststap01.corp.alldata.net/webservices/CoreService2011.svc/basicHttp"
        binding="basicHttpBinding" bindingConfiguration="basicHttp"
        contract="CoreService2011.ICoreService" name="basicHttp" />
      <endpoint address="http://vmrecmststap01.corp.alldata.net/webservices/CoreService2011.svc/streamDownload_basicHttp"
        binding="basicHttpBinding" bindingConfiguration="streamDownload_basicHttp"
        contract="CoreService2011.IStreamDownload" name="streamDownload_basicHttp" />
      <endpoint address="http://vmrecmststap01.corp.alldata.net/webservices/CoreService2011.svc/streamUpload_basicHttp"
        binding="basicHttpBinding" bindingConfiguration="streamUpload_basicHttp"
        contract="CoreService2011.IStreamUpload" name="streamUpload_basicHttp" />
      <endpoint address="http://vmrecmststap01.corp.alldata.net/webservices/CoreService2011.svc/wsHttp"
        binding="wsHttpBinding" bindingConfiguration="wsHttp" contract="CoreService2011.ISessionAwareCoreService"
        name="wsHttp">
        <identity>
          <dns value="localhost" />
        </identity>
      </endpoint>
    </client>
  </system.serviceModel>

</configuration>
