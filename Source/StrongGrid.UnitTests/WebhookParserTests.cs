using Newtonsoft.Json;
using Shouldly;
using StrongGrid.Models;
using StrongGrid.Models.Webhooks;
using StrongGrid.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace StrongGrid.UnitTests
{
	public class WebhookParserTests
	{
		#region FIELDS

		private const string PROCESSED_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'pool': {
				'name':'new_MY_test',
				'id':210
			},
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'processed',
			'category':'cat facts',
			'sg_event_id':'rbtnWrG1DVDGGGFHFyun0A==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.000000000000000000000',
			'asm_group_id':123456
		}";

		private const string BOUNCED_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'bounce',
			'category':'cat facts',
			'sg_event_id':'6g4ZI7SA-xmRDv57GoPIPw==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0',
			'reason':'500 unknown recipient',
			'status':'5.0.0',
			'type':'bounce'
		}";

		private const string DEFERRED_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'deferred',
			'category':'cat facts',
			'sg_event_id':'t7LEShmowp86DTdUW8M-GQ==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0',
			'response':'400 try again later',
			'attempt':'5'
		}";

		private const string DROPPED_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'dropped',
			'category':'cat facts',
			'sg_event_id':'zmzJhfJgAfUSOW80yEbPyw==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0',
			'reason':'Bounced Address',
			'status':'5.0.0'
		}";

		private const string BLOCKED_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'bounce',
			'category':'cat facts',
			'sg_event_id':'6g4ZI7SA-xmRDv57GoPIPw==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0',
			'reason':'500 unknown recipient',
			'status':'5.0.0',
			'type':'blocked'
		}";

		private const string DELIVERED_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'delivered',
			'category':'cat facts',
			'sg_event_id':'rWVYmVk90MjZJ9iohOBa3w==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0',
			'response':'250 OK'
		}";

		private const string CLICKED_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'click',
			'category':'cat facts',
			'sg_event_id':'kCAi1KttyQdEKHhdC-nuEA==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0',
			'useragent':'Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)',
			'ip':'255.255.255.255',
			'url':'http://www.sendgrid.com/'
		}";

		private const string OPENED_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'open',
			'category':'cat facts',
			'sg_event_id':'FOTFFO0ecsBE-zxFXfs6WA==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0',
			'useragent':'Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)',
			'ip':'255.255.255.255'
		}";

		private const string SPAMREPORT_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'spamreport',
			'category':'cat facts',
			'sg_event_id':'37nvH5QBz858KGVYCM4uOA==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0'
		}";

		private const string UNSUBSCRIBE_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'unsubscribe',
			'category':'cat facts',
			'sg_event_id':'zz_BjPgU_5pS-J8vlfB1sg==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0'
		}";

		private const string GROUPUNSUBSCRIBE_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'group_unsubscribe',
			'category':'cat facts',
			'sg_event_id':'ahSCB7xYcXFb-hEaawsPRw==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0',
			'useragent':'Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)',
			'ip':'255.255.255.255',
			'url':'http://www.sendgrid.com/',
			'asm_group_id':10
		}";

		private const string GROUPRESUBSCRIBE_JSON = @"
		{
			'email':'example@test.com',
			'timestamp':1513299569,
			'smtp-id':'<14c5d75ce93.dfd.64b469@ismtpd-555>',
			'event':'group_resubscribe',
			'category':'cat facts',
			'sg_event_id':'w_u0vJhLT-OFfprar5N93g==',
			'sg_message_id':'14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0',
			'useragent':'Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)',
			'ip':'255.255.255.255',
			'url':'http://www.sendgrid.com/',
			'asm_group_id':10
		}";

		private const string INBOUND_EMAIL_WEBHOOK = @"--xYzZY
Content-Disposition: form-data; name=""dkim""

{@hotmail.com : pass}
--xYzZY
Content-Disposition: form-data; name=""envelope""

{""to"":[""test@api.yourdomain.com""],""from"":""bob@example.com""}
--xYzZY
Content-Disposition: form-data; name=""subject""

Test #1
--xYzZY
Content-Disposition: form-data; name=""charsets""

{""to"":""UTF-8"",""html"":""us-ascii"",""subject"":""UTF-8"",""from"":""UTF-8"",""text"":""us-ascii""}
--xYzZY
Content-Disposition: form-data; name=""SPF""

softfail
--xYzZY
Content-Disposition: form-data; name=""headers""

Received: by mx0036p1las1.sendgrid.net with SMTP id JtK4a8OKW4 Wed, 25 Oct 2017 22:36:45 +0000 (UTC)
Received: from NAM03-BY2-obe.outbound.protection.outlook.com (unknown[10.43.24.23]) by mx0036p1las1.sendgrid.net (Postfix) with ESMTPS id 210E420432A for <test @api.yourdomain.com>; Wed, 25 Oct 2017 22:36:44 +0000 (UTC)
DKIM-Signature: v=1; a=rsa-sha256; c=relaxed/relaxed; d=hotmail.com; s=selector1; h=From:Date:Subject:Message-ID:Content-Type:MIME-Version; bh=v+swJ1aNYbEcg9bJpD94LKJL7bPGzXmyfchznPUUm3o=; b=Xd5Nx/eKt5gGYAhtLt4cPR4V+3lIbuaCTK+NeDBE61haLrmOS3h66woY27Rofk6bqpoVzlhq8qqtX3wp7cGaslDSYbOMKXJ0T7mn56/BVhIcVyNuz0PSNbTEKAQHoJzwVbp3b4VU/H3ZYgNVlYoSgDtyC3n52u2GtAYokEbvYRs1v501tCsf5MDZCVav9XYOb7TsvOHDca6SjX4n7rokHIovaPEp86gy2xxAz+EisUngYzJ3WFH1yLTNcsTvHJL+S/IBDR73ZgWX2PLo9lh0SdR/F5/wLhkaOFlyCD7oSRPBOBJgfZKtmZGh5P2e/hI0X1THRM4Fl3rRLWwrdGUA4Q==
Received: from BY2NAM03FT010.eop-NAM03.prod.protection.outlook.com (10.152.84.60) by BY2NAM03HT109.eop-NAM03.prod.protection.outlook.com (10.152.85.95) with Microsoft SMTP Server(version= TLS1_2, cipher= TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384_P384) id 15.20.178.5; Wed, 25 Oct 2017 22:36:43 +0000
Received: from BY2PR04MB1989.namprd04.prod.outlook.com(10.152.84.59) by BY2NAM03FT010.mail.protection.outlook.com(10.152.84.122) with Microsoft SMTP Server(version= TLS1_2, cipher= TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256_P256) id 15.20.178.5 via Frontend Transport; Wed, 25 Oct 2017 22:36:43 +0000
Received: from BY2PR04MB1989.namprd04.prod.outlook.com([10.166.111.17]) by BY2PR04MB1989.namprd04.prod.outlook.com([10.166.111.17]) with mapi id 15.20.0156.007; Wed, 25 Oct 2017 22:36:43 +0000
From: Bob Smith<bob@example.com>
To: ""Test Recipient"" <test @api.yourdomain.com>
Subject: Test #1
Thread-Topic: Test #1
Thread-Index: AdNN4bXcGTp4NdbjRHGNZNWefHx/Gg==
Date: Wed, 25 Oct 2017 22:36:43 +0000
Message-ID: <BY2PR04MB19890F956D5521DA3B25F85BCD440 @BY2PR04MB1989.namprd04.prod.outlook.com>
Accept-Language: en-US
Content-Language: en-US
X-MS-Has-Attach:
X-MS-TNEF-Correlator:
x-incomingtopheadermarker: OriginalChecksum:6E601097DDEC6FC57E546B55598497FC1691DF179D48A390E1D00BF5D97DCC85; UpperCasedChecksum:B7D711ACA2768A91C1F2659CAA244146EA1FF640E75765587AEE1BF8F9E27CE4;SizeAsReceived:6758;Count:43
x-tmn: [fhxQvwKmgzB/h6K9GZxYNsdaZLsNk3xU]
x-ms-publictraffictype: Email
x-microsoft-exchange-diagnostics: 1;BY2NAM03HT109;7:bk3VKGh39PrHhS2gX1krsQuu0T2egofcgz52wEPlumRalehclvcU2NX5JzDMUszjK8+hlQovvIJ/KfP1R+INiCo1Sn+FtXBForMk0IDaECwdb9Z4ceCxF/D0eOnAMvEHSXbK17Tcyy0RpNyBuVCuNyNO9YU0IxU+8ff8Le8CnP/TwM+ae22nkJy74bMcBYGFm5x2J7w/JRCKR9m9+CZ8nor9RFfWL7WPHjBKCfGzexck6IYLGJ3+T6AD5x8cfe1+
x-incomingheadercount: 43
x-eopattributedmessage: 0
x-ms-office365-filtering-correlation-id: 7806427c-f04d-469f-e178-08d51bf8de1f
x-microsoft-antispam: UriScan:;BCL:0;PCL:0;RULEID:(22001)(201702061074)(5061506573)(5061507331)(1603103135)(2017031320274)(2017031324274)(2017031323274)(2017031322404)(1603101448)(1601125374)(1701031045);SRVR:BY2NAM03HT109;
x-ms-traffictypediagnostic: BY2NAM03HT109:
x-exchange-antispam-report-test: UriScan:(21748063052155);
x-exchange-antispam-report-cfa-test: BCL:0;PCL:0;RULEID:(100000700101)(100105000095)(100000701101)(100105300095)(100000702101)(100105100095)(444000031);SRVR:BY2NAM03HT109;BCL:0;PCL:0;RULEID:(100000800101)(100110000095)(100000801101)(100110300095)(100000802101)(100110100095)(100000803101)(100110400095)(100000804101)(100110200095)(100000805101)(100110500095);SRVR:BY2NAM03HT109;
x-forefront-prvs: 0471B73328
x-forefront-antispam-report: SFV:NSPM;SFS:(7070007)(98901004);DIR:OUT;SFP:1901;SCL:1;SRVR:BY2NAM03HT109;H:BY2PR04MB1989.namprd04.prod.outlook.com;FPR:;SPF:None;LANG:;
spamdiagnosticoutput: 1:99
spamdiagnosticmetadata: NSPM
Content-Type: multipart/alternative; boundary=""_000_BY2PR04MB19890F956D5521DA3B25F85BCD440BY2PR04MB1989namp_""
MIME-Version: 1.0
X-OriginatorOrg: hotmail.com
X-MS-Exchange-CrossTenant-Network-Message-Id: 7806427c-f04d-469f-e178-08d51bf8de1f
X-MS-Exchange-CrossTenant-originalarrivaltime: 25 Oct 2017 22:36:43.4326 (UTC)
X-MS-Exchange-CrossTenant-fromentityheader: Internet
X-MS-Exchange-CrossTenant-id: 84df9e7f-e9f6-40af-b435-aaaaaaaaaaaa
X-MS-Exchange-Transport-CrossTenantHeadersStamped: BY2NAM03HT109

--xYzZY
Content-Disposition: form-data; name=""to""

""Test Recipient"" <test@api.yourdomain.com>
--xYzZY
Content-Disposition: form-data; name=""html""

<html xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"" xmlns:w=""urn:schemas-microsoft-com:office:word"" xmlns:m=""http://schemas.microsoft.com/office/2004/12/omml"" xmlns=""http://www.w3.org/TR/REC-html40"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=us-ascii"">
<meta name = ""Generator"" content=""Microsoft Word 15 (filtered medium)"">
<style><!--
/* Font Definitions */
@font-face
	{font-family:""Cambria Math"";
	panose-1:2 4 5 3 5 4 6 3 2 4;}
@font-face
	{font-family:Calibri;
	panose-1:2 15 5 2 2 2 4 3 2 4;}
/* Style Definitions */
p.MsoNormal, li.MsoNormal, div.MsoNormal
	{margin:0in;
	margin-bottom:.0001pt;
	font-size:11.0pt;
	font-family:""Calibri"",sans-serif;}
a:link, span.MsoHyperlink
	{mso-style-priority:99;
	color:#0563C1;
	text-decoration:underline;}
a:visited, span.MsoHyperlinkFollowed
	{mso-style-priority:99;
	color:#954F72;
	text-decoration:underline;}
span.EmailStyle17
	{mso-style-type:personal-compose;
	font-family:""Calibri"",sans-serif;
	color:windowtext;}
.MsoChpDefault
	{mso-style-type:export-only;
	font-family:""Calibri"",sans-serif;}
@page WordSection1
{
	size:8.5in 11.0in;
	margin:1.0in 1.0in 1.0in 1.0in;
}
div.WordSection1
	{page:WordSection1;}
--></style><!--[if gte mso 9]><xml>
<o:shapedefaults v:ext=""edit"" spidmax=""1026"" />
</xml><![endif]--><!--[if gte mso 9]><xml>
<o:shapelayout v:ext=""edit"">
<o:idmap v:ext=""edit"" data=""1"" />
</o:shapelayout></xml><![endif]-->
</head>
<body lang = ""EN-US"" link=""#0563C1"" vlink=""#954F72"">
<div class=""WordSection1"">
<p class=""MsoNormal"">Test #1<o:p></o:p></p>
</div>
</body>
</html>

--xYzZY
Content-Disposition: form-data; name=""from""

Bob Smith<bob@example.com>
--xYzZY
Content-Disposition: form-data; name=""text""

Test #1

--xYzZY
Content-Disposition: form-data; name=""sender_ip""

10.43.24.23
--xYzZY
Content-Disposition: form-data; name=""attachments""

0
--xYzZY--";

		// SendGrid uses a very simplistic payload in several of their client libraries to test event webhook signing.
		// The JSON string in the payload is simplistic and does not represent a typical event you would get in a webhook.
		// For reference, here's where you can find this simplistic payload in the various client libraries:
		//   - JAVA: https://github.com/sendgrid/sendgrid-java/blob/2104e0e10021107b76f7d2638e2a74d6f6e1c228/examples/helpers/eventwebhook/Example.java
		//   - GO: https://github.com/sendgrid/sendgrid-go/blob/640cd9b866da50f245322a051c66aacc519dcaf1/helpers/eventwebhook/eventwebhook_test.go
		//   - NODEJS: https://github.com/sendgrid/sendgrid-nodejs/blob/f0951298f3f545fd253857d6824ec3e63b37d17d/test/typescript/eventwebhook.ts
		//   - PHP: https://github.com/sendgrid/sendgrid-php/blob/eb22165d7f/test/unit/EventWebhookTest.php
		//   - RUBY: https://github.com/sendgrid/sendgrid-ruby/blob/53c6c05fe7e3cd42f0ba3b5f436f3aef94a7e70b/spec/sendgrid/helpers/eventwebhook/eventwebhook_spec.rb
		private const string SENDGRID_PUBLIC_KEY = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEEDr2LjtURuePQzplybdC+u4CwrqDqBaWjcMMsTbhdbcwHBcepxo7yAQGhHPTnlvFYPAZFceEu/1FwCM/QmGUhA==";
		private const string SENDGRID_PAYLOAD = "{\"category\":\"example_payload\",\"event\":\"test_event\",\"message_id\":\"message_id\"}";
		private const string SENDGRID_SIGNATURE = "MEUCIQCtIHJeH93Y+qpYeWrySphQgpNGNr/U+UyUlBkU6n7RAwIgJTz2C+8a8xonZGi6BpSzoQsbVRamr2nlxFDWYNH2j/0=";
		private const string SENDGRID_TIMESTAMP = "1588788367";

		// This is a more realistic payload which contains an array of events.
		// I obtained the sample payload, signature and timestamp by POST'ing a request to https://api.sendgrid.com/v3/user/webhooks/event/test
		private const string MY_PUBLIC_KEY = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE2is1eViXeZ9NwNbYKD/b51+WBZQVf+mLT0QCLiD6+HgWlNkrldvci/3m/o72GgCr3ilINxo9FpHElSHNnlYA7A==";
		private const string MY_PAYLOAD = "[{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"processed\",\"category\":\"cat facts\",\"sg_event_id\":\"-w3n3K8nBCtORXZVD9wxWQ==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\"},{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"deferred\",\"category\":\"cat facts\",\"sg_event_id\":\"27_-LKWAeSzRe_JdEA8N8g==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\",\"response\":\"400 try again later\",\"attempt\":\"5\"},{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"delivered\",\"category\":\"cat facts\",\"sg_event_id\":\"Vx4gXYiwyWNyGDRtSn-RWQ==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\",\"response\":\"250 OK\"},{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"open\",\"category\":\"cat facts\",\"sg_event_id\":\"hdB0Dxh27lTQDmSsN9-3zg==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\",\"useragent\":\"Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)\",\"ip\":\"255.255.255.255\"},{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"click\",\"category\":\"cat facts\",\"sg_event_id\":\"eQbgQ_Aff12HIhnwf3aN_A==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\",\"useragent\":\"Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)\",\"ip\":\"255.255.255.255\",\"url\":\"http://www.sendgrid.com/\"},{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"bounce\",\"category\":\"cat facts\",\"sg_event_id\":\"mPsgXgr8qq1cWHFirZCoSA==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\",\"reason\":\"500 unknown recipient\",\"status\":\"5.0.0\"},{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"dropped\",\"category\":\"cat facts\",\"sg_event_id\":\"K1pmJN0j0TrvQ8VJza5mxg==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\",\"reason\":\"Bounced Address\",\"status\":\"5.0.0\"},{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"spamreport\",\"category\":\"cat facts\",\"sg_event_id\":\"k0y94nBNOaWkc0XTiTB53g==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\"},{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"unsubscribe\",\"category\":\"cat facts\",\"sg_event_id\":\"LgPxePMfafBIVUv_HLF0ZQ==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\"},{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"group_unsubscribe\",\"category\":\"cat facts\",\"sg_event_id\":\"GPB7vO-DgotTjSxE7Odpew==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\",\"useragent\":\"Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)\",\"ip\":\"255.255.255.255\",\"url\":\"http://www.sendgrid.com/\",\"asm_group_id\":10},{\"email\":\"example@test.com\",\"timestamp\":1592925285,\"smtp-id\":\"\u003c14c5d75ce93.dfd.64b469@ismtpd-555\u003e\",\"event\":\"group_resubscribe\",\"category\":\"cat facts\",\"sg_event_id\":\"W5Bm2KOpz0eNjj1qX7Yl6A==\",\"sg_message_id\":\"14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0\",\"useragent\":\"Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)\",\"ip\":\"255.255.255.255\",\"url\":\"http://www.sendgrid.com/\",\"asm_group_id\":10}]";
		private const string MY_SIGNATURE = "MEUCIQCkVB8ZeiGaWA6o3/PGnqNQgdqzOCERs6w999YTquAiDQIgdTAvHUk6+HMzLI//7NHWfIROPg//P+ZAo17QISy8Y1U=";
		private const string MY_TIMESTAMP = "1592925285";

		// This public key, payload ans signature was taken from a StackOverflow article 
		// FROM https://stackoverflow.com/questions/59078889/ecdsa-verify-signature-in-c-sharp-using-public-key-and-signature-from-java
		private const string STACKOVERFLOW_PUBLIC_KEY = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAExeg15CVOUcspdO0Pm27hPVx50thn0CGk3/3NLl08qcK+0U7cesOUUwxQetMgtUHrh0lNao5XRAAurhcBtZpo6w==";
		private const string STACKOVERFLOW_PAYLOAD = "ABCDEFGH";
		private const string STACKOVERFLOW_SIGNATURE = "MEQCIFNEZQRzIrvr6dtJ4j4HP8nXHSts3w3qsRt8cFXBaOGAAiAJO/EjzCZlNLQSvKBinVHfSvTEmor0dc3YX7FPMnnYCg==";
		private const string STACKOVERFLOW_TIMESTAMP = "";

		#endregion

		[Fact]
		public void InboundEmail()
		{
			// Arrange
			var parser = new WebhookParser();
			using (var stream = GetStream(INBOUND_EMAIL_WEBHOOK))
			{
				// Act
				var inboundEmail = parser.ParseInboundEmailWebhook(stream);

				// Assert
				inboundEmail.Attachments.ShouldNotBeNull();
				inboundEmail.Attachments.Length.ShouldBe(0);
				inboundEmail.Cc.ShouldNotBeNull();
				inboundEmail.Cc.Length.ShouldBe(0);
				inboundEmail.Charsets.ShouldNotBeNull();
				inboundEmail.Charsets.Length.ShouldBe(5);
				inboundEmail.Dkim.ShouldBe("{@hotmail.com : pass}");
				inboundEmail.From.ShouldNotBeNull();
				inboundEmail.From.Email.ShouldBe("bob@example.com");
				inboundEmail.From.Name.ShouldBe("Bob Smith");
				inboundEmail.Headers.ShouldNotBeNull();
				inboundEmail.Headers.Length.ShouldBe(40);
				inboundEmail.Html.ShouldStartWith("<html", Case.Insensitive);
				inboundEmail.SenderIp.ShouldBe("10.43.24.23");
				inboundEmail.SpamReport.ShouldBeNull();
				inboundEmail.SpamScore.ShouldBeNull();
				inboundEmail.Spf.ShouldBe("softfail");
				inboundEmail.Subject.ShouldBe("Test #1");
				inboundEmail.Text.ShouldBe("Test #1\r\n");
				inboundEmail.To.ShouldNotBeNull();
				inboundEmail.To.Length.ShouldBe(1);
				inboundEmail.To[0].Email.ShouldBe("test@api.yourdomain.com");
				inboundEmail.To[0].Name.ShouldBe("Test Recipient");
			}
		}

		[Fact]
		public async Task InboundEmailAsync()
		{
			// Arrange
			var parser = new WebhookParser();
			using (var stream = GetStream(INBOUND_EMAIL_WEBHOOK))
			{
				// Act
				var inboundEmail = await parser.ParseInboundEmailWebhookAsync(stream).ConfigureAwait(false);

				// Assert
				inboundEmail.Attachments.ShouldNotBeNull();
				inboundEmail.Attachments.Length.ShouldBe(0);
				inboundEmail.Cc.ShouldNotBeNull();
				inboundEmail.Cc.Length.ShouldBe(0);
				inboundEmail.Charsets.ShouldNotBeNull();
				inboundEmail.Charsets.Length.ShouldBe(5);
				inboundEmail.Dkim.ShouldBe("{@hotmail.com : pass}");
				inboundEmail.From.ShouldNotBeNull();
				inboundEmail.From.Email.ShouldBe("bob@example.com");
				inboundEmail.From.Name.ShouldBe("Bob Smith");
				inboundEmail.Headers.ShouldNotBeNull();
				inboundEmail.Headers.Length.ShouldBe(40);
				inboundEmail.Html.ShouldStartWith("<html", Case.Insensitive);
				inboundEmail.SenderIp.ShouldBe("10.43.24.23");
				inboundEmail.SpamReport.ShouldBeNull();
				inboundEmail.SpamScore.ShouldBeNull();
				inboundEmail.Spf.ShouldBe("softfail");
				inboundEmail.Subject.ShouldBe("Test #1");
				inboundEmail.Text.ShouldBe("Test #1\r\n");
				inboundEmail.To.ShouldNotBeNull();
				inboundEmail.To.Length.ShouldBe(1);
				inboundEmail.To[0].Email.ShouldBe("test@api.yourdomain.com");
				inboundEmail.To[0].Name.ShouldBe("Test Recipient");
			}
		}

		[Fact]
		public void RawPayloadWithAttachments()
		{
			var parser = new WebhookParser();

			using (Stream stream = new MemoryStream())
			{
				using (var fileStream = File.OpenRead("InboudEmailTestData/raw_data.txt"))
				{
					fileStream.CopyTo(stream);
				}
				stream.Position = 0;

				InboundEmail inboundEmail = parser.ParseInboundEmailWebhook(stream);

				inboundEmail.ShouldNotBeNull();

				inboundEmail.Dkim.ShouldBe("{@sendgrid.com : pass}");

				var rawEmailTestData = File.ReadAllText("InboudEmailTestData/raw_email.txt");
				inboundEmail.RawEmail.Trim().ShouldBe(rawEmailTestData);

				inboundEmail.To[0].Email.ShouldBe("inbound@inbound.example.com");
				inboundEmail.To[0].Name.ShouldBe(string.Empty);

				inboundEmail.Cc.Length.ShouldBe(0);

				inboundEmail.From.Email.ShouldBe("test@example.com");
				inboundEmail.From.Name.ShouldBe("Example User");

				inboundEmail.SenderIp.ShouldBe("0.0.0.0");

				inboundEmail.SpamReport.ShouldBeNull();

				inboundEmail.Envelope.From.ShouldBe("test@example.com");
				inboundEmail.Envelope.To.Length.ShouldBe(1);
				inboundEmail.Envelope.To.ShouldContain("inbound@inbound.example.com");

				inboundEmail.Subject.ShouldBe("Raw Payload");

				inboundEmail.SpamScore.ShouldBeNull();

				inboundEmail.Charsets.Except(new[]
				{
					new KeyValuePair<string, Encoding>("to", Encoding.UTF8),
					new KeyValuePair<string, Encoding>("subject", Encoding.UTF8),
					new KeyValuePair<string, Encoding>("from", Encoding.UTF8)
				}).Count().ShouldBe(0);

				inboundEmail.Spf.ShouldBe("pass");
			}
		}

		[Fact]
		public async Task RawPayloadWithAttachmentsAsync()
		{
			var parser = new WebhookParser();

			using (Stream stream = new MemoryStream())
			{
				using (var fileStream = File.OpenRead("InboudEmailTestData/raw_data.txt"))
				{
					await fileStream.CopyToAsync(stream).ConfigureAwait(false);
				}
				stream.Position = 0;

				InboundEmail inboundEmail = await parser.ParseInboundEmailWebhookAsync(stream).ConfigureAwait(false);

				inboundEmail.ShouldNotBeNull();

				inboundEmail.Dkim.ShouldBe("{@sendgrid.com : pass}");

				var rawEmailTestData = File.ReadAllText("InboudEmailTestData/raw_email.txt");
				inboundEmail.RawEmail.Trim().ShouldBe(rawEmailTestData);

				inboundEmail.To[0].Email.ShouldBe("inbound@inbound.example.com");
				inboundEmail.To[0].Name.ShouldBe(string.Empty);

				inboundEmail.Cc.Length.ShouldBe(0);

				inboundEmail.From.Email.ShouldBe("test@example.com");
				inboundEmail.From.Name.ShouldBe("Example User");

				inboundEmail.SenderIp.ShouldBe("0.0.0.0");

				inboundEmail.SpamReport.ShouldBeNull();

				inboundEmail.Envelope.From.ShouldBe("test@example.com");
				inboundEmail.Envelope.To.Length.ShouldBe(1);
				inboundEmail.Envelope.To.ShouldContain("inbound@inbound.example.com");

				inboundEmail.Subject.ShouldBe("Raw Payload");

				inboundEmail.SpamScore.ShouldBeNull();

				inboundEmail.Charsets.Except(new[]
				{
					new KeyValuePair<string, Encoding>("to", Encoding.UTF8),
					new KeyValuePair<string, Encoding>("subject", Encoding.UTF8),
					new KeyValuePair<string, Encoding>("from", Encoding.UTF8)
				}).Count().ShouldBe(0);

				inboundEmail.Spf.ShouldBe("pass");
			}
		}

		[Fact]
		public void Parse_processed_JSON()
		{
			// Arrange

			// Act
			var result = (ProcessedEvent)JsonConvert.DeserializeObject<Event>(PROCESSED_JSON, new WebHookEventConverter());

			// Assert
			result.AsmGroupId.ShouldBe(123456);
			result.Categories.Length.ShouldBe(1);
			result.Categories[0].ShouldBe("cat facts");
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.Processed);
			result.InternalEventId.ShouldBe("rbtnWrG1DVDGGGFHFyun0A==");
			result.InternalMessageId.ShouldBe("14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.000000000000000000000");
			result.IpPool.ShouldNotBeNull();
			result.IpPool.Id.ShouldBe(210);
			result.IpPool.Name.ShouldBe("new_MY_test");
			result.MessageId.ShouldBe("14c5d75ce93.dfd.64b469");
			result.SmtpId.ShouldBe("<14c5d75ce93.dfd.64b469@ismtpd-555>");
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public void Parse_bounced_JSON()
		{
			// Arrange

			// Act
			var result = (BouncedEvent)JsonConvert.DeserializeObject<Event>(BOUNCED_JSON, new WebHookEventConverter());

			// Assert
			result.Categories.Length.ShouldBe(1);
			result.Categories[0].ShouldBe("cat facts");
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.Bounce);
			result.InternalEventId.ShouldBe("6g4ZI7SA-xmRDv57GoPIPw==");
			result.InternalMessageId.ShouldBe("14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0");
			result.MessageId.ShouldBe("14c5d75ce93.dfd.64b469");
			result.Reason.ShouldBe("500 unknown recipient");
			result.SmtpId.ShouldBe("<14c5d75ce93.dfd.64b469@ismtpd-555>");
			result.Status.ShouldBe("5.0.0");
			result.Type.ShouldBe(StrongGrid.Models.Webhooks.BounceType.HardBounce);
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public void Parse_deferred_JSON()
		{
			// Arrange

			// Act
			var result = (DeferredEvent)JsonConvert.DeserializeObject<Event>(DEFERRED_JSON, new WebHookEventConverter());

			// Assert
			result.AsmGroupId.ShouldBeNull();
			result.Attempts.ShouldBe(5);
			result.Categories.Length.ShouldBe(1);
			result.Categories[0].ShouldBe("cat facts");
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.Deferred);
			result.InternalEventId.ShouldBe("t7LEShmowp86DTdUW8M-GQ==");
			result.InternalMessageId.ShouldBe("14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0");
			result.MessageId.ShouldBe("14c5d75ce93.dfd.64b469");
			result.SmtpId.ShouldBe("<14c5d75ce93.dfd.64b469@ismtpd-555>");
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public void Parse_dropped_JSON()
		{
			// Arrange

			// Act
			var result = (DroppedEvent)JsonConvert.DeserializeObject<Event>(DROPPED_JSON, new WebHookEventConverter());

			// Assert
			result.Categories.Length.ShouldBe(1);
			result.Categories[0].ShouldBe("cat facts");
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.Dropped);
			result.InternalEventId.ShouldBe("zmzJhfJgAfUSOW80yEbPyw==");
			result.InternalMessageId.ShouldBe("14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0");
			result.MessageId.ShouldBe("14c5d75ce93.dfd.64b469");
			result.Reason.ShouldBe("Bounced Address");
			result.SmtpId.ShouldBe("<14c5d75ce93.dfd.64b469@ismtpd-555>");
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public void Parse_delivered_JSON()
		{
			// Arrange

			// Act
			var result = (DeliveredEvent)JsonConvert.DeserializeObject<Event>(DELIVERED_JSON, new WebHookEventConverter());

			// Assert
			result.AsmGroupId.ShouldBeNull();
			result.Categories.Length.ShouldBe(1);
			result.Categories[0].ShouldBe("cat facts");
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.Delivered);
			result.InternalEventId.ShouldBe("rWVYmVk90MjZJ9iohOBa3w==");
			result.InternalMessageId.ShouldBe("14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0");
			result.MessageId.ShouldBe("14c5d75ce93.dfd.64b469");
			result.Response.ShouldBe("250 OK");
			result.SmtpId.ShouldBe("<14c5d75ce93.dfd.64b469@ismtpd-555>");
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public void Parse_clicked_JSON()
		{
			// Arrange

			// Act
			var result = (ClickedEvent)JsonConvert.DeserializeObject<Event>(CLICKED_JSON, new WebHookEventConverter());

			// Assert
			result.Categories.Length.ShouldBe(1);
			result.Categories[0].ShouldBe("cat facts");
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.Click);
			result.InternalEventId.ShouldBe("kCAi1KttyQdEKHhdC-nuEA==");
			result.InternalMessageId.ShouldBe("14c5d75ce93.dfd.64b469.filter0001.16648.5515E0B88.0");
			result.IpAddress.ShouldBe("255.255.255.255");
			result.MessageId.ShouldBe("14c5d75ce93.dfd.64b469");
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.Url.ShouldBe("http://www.sendgrid.com/");
			result.UserAgent.ShouldBe("Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)");
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public void Parse_opened_JSON()
		{
			// Arrange

			// Act
			var result = (OpenedEvent)JsonConvert.DeserializeObject<Event>(OPENED_JSON, new WebHookEventConverter());

			// Assert
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.Open);
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.UserAgent.ShouldBe("Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)");
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public void Parse_spamreport_JSON()
		{
			// Arrange

			// Act
			var result = (SpamReportEvent)JsonConvert.DeserializeObject<Event>(SPAMREPORT_JSON, new WebHookEventConverter());

			// Assert
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.SpamReport);
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public void Parse_unsubscribe_JSON()
		{
			// Arrange

			// Act
			var result = (UnsubscribeEvent)JsonConvert.DeserializeObject<Event>(UNSUBSCRIBE_JSON, new WebHookEventConverter());

			// Assert
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.Unsubscribe);
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public void Parse_groupunsubscribe_JSON()
		{
			// Arrange

			// Act
			var result = (GroupUnsubscribeEvent)JsonConvert.DeserializeObject<Event>(GROUPUNSUBSCRIBE_JSON, new WebHookEventConverter());

			// Assert
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.GroupUnsubscribe);
			result.IpAddress.ShouldBe("255.255.255.255");
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.UserAgent.ShouldBe("Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)");
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public void Parse_groupresubscribe_JSON()
		{
			// Arrange

			// Act
			var result = (GroupResubscribeEvent)JsonConvert.DeserializeObject<Event>(GROUPRESUBSCRIBE_JSON, new WebHookEventConverter());

			// Assert
			result.Email.ShouldBe("example@test.com");
			result.EventType.ShouldBe(EventType.GroupResubscribe);
			result.IpAddress.ShouldBe("255.255.255.255");
			result.Timestamp.ToUnixTime().ShouldBe(1513299569);
			result.UserAgent.ShouldBe("Mozilla/4.0 (compatible; MSIE 6.1; Windows XP; .NET CLR 1.1.4322; .NET CLR 2.0.50727)");
			result.UniqueArguments.ShouldNotBeNull();
			result.UniqueArguments.Count.ShouldBe(0);
		}

		[Fact]
		public async Task Processed()
		{
			// Arrange
			var responseContent = $"[{PROCESSED_JSON}]";
			var parser = new WebhookParser();
			using (var stream = GetStream(responseContent))
			{
				// Act
				var result = await parser.ParseEventsWebhookAsync(stream).ConfigureAwait(false);

				// Assert
				result.ShouldNotBeNull();
				result.Length.ShouldBe(1);
				result[0].GetType().ShouldBe(typeof(ProcessedEvent));
			}
		}

		[Fact]
		public async Task Bounced()
		{
			// Arrange
			var responseContent = $"[{BOUNCED_JSON}]";
			var parser = new WebhookParser();
			using (var stream = GetStream(responseContent))
			{
				// Act
				var result = await parser.ParseEventsWebhookAsync(stream).ConfigureAwait(false);

				// Assert
				result.ShouldNotBeNull();
				result.Length.ShouldBe(1);
				result[0].GetType().ShouldBe(typeof(BouncedEvent));
			}
		}

		[Fact]
		public async Task Deferred()
		{
			// Arrange
			var responseContent = $"[{DEFERRED_JSON}]";
			var parser = new WebhookParser();
			using (var stream = GetStream(responseContent))
			{
				// Act
				var result = await parser.ParseEventsWebhookAsync(stream).ConfigureAwait(false);

				// Assert
				result.ShouldNotBeNull();
				result.Length.ShouldBe(1);
				result[0].GetType().ShouldBe(typeof(DeferredEvent));
			}
		}

		[Fact]
		public async Task Dropped()
		{
			// Arrange
			var responseContent = $"[{DROPPED_JSON}]";
			var parser = new WebhookParser();
			using (var stream = GetStream(responseContent))
			{
				// Act
				var result = await parser.ParseEventsWebhookAsync(stream).ConfigureAwait(false);

				// Assert
				result.ShouldNotBeNull();
				result.Length.ShouldBe(1);
				result[0].GetType().ShouldBe(typeof(DroppedEvent));
			}
		}

		[Fact]
		public async Task Clicked()
		{
			// Arrange
			var responseContent = $"[{CLICKED_JSON}]";
			var parser = new WebhookParser();
			using (var stream = GetStream(responseContent))
			{
				// Act
				var result = await parser.ParseEventsWebhookAsync(stream).ConfigureAwait(false);

				// Assert
				result.ShouldNotBeNull();
				result.Length.ShouldBe(1);
				result[0].GetType().ShouldBe(typeof(ClickedEvent));
			}
		}

		[Fact]
		public async Task Opened()
		{
			// Arrange
			var responseContent = $"[{OPENED_JSON}]";
			var parser = new WebhookParser();
			using (var stream = GetStream(responseContent))
			{
				// Act
				var result = await parser.ParseEventsWebhookAsync(stream).ConfigureAwait(false);

				// Assert
				result.ShouldNotBeNull();
				result.Length.ShouldBe(1);
				result[0].GetType().ShouldBe(typeof(OpenedEvent));
			}
		}

		[Fact]
		public async Task Unsubscribe()
		{
			// Arrange
			var responseContent = $"[{UNSUBSCRIBE_JSON}]";
			var parser = new WebhookParser();
			using (var stream = GetStream(responseContent))
			{
				// Act
				var result = await parser.ParseEventsWebhookAsync(stream).ConfigureAwait(false);

				// Assert
				result.ShouldNotBeNull();
				result.Length.ShouldBe(1);
				result[0].GetType().ShouldBe(typeof(UnsubscribeEvent));
			}
		}

		[Fact]
		public async Task GroupUnsubscribe()
		{
			// Arrange
			var responseContent = $"[{GROUPUNSUBSCRIBE_JSON}]";
			var parser = new WebhookParser();
			using (var stream = GetStream(responseContent))
			{
				// Act
				var result = await parser.ParseEventsWebhookAsync(stream).ConfigureAwait(false);

				// Assert
				result.ShouldNotBeNull();
				result.Length.ShouldBe(1);
				result[0].GetType().ShouldBe(typeof(GroupUnsubscribeEvent));
			}
		}

		[Fact]
		public async Task GroupResubscribe()
		{
			// Arrange
			var responseContent = $"[{GROUPRESUBSCRIBE_JSON}]";
			var parser = new WebhookParser();
			using (var stream = GetStream(responseContent))
			{
				// Act
				var result = await parser.ParseEventsWebhookAsync(stream).ConfigureAwait(false);

				// Assert
				result.ShouldNotBeNull();
				result.Length.ShouldBe(1);
				result[0].GetType().ShouldBe(typeof(GroupResubscribeEvent));
			}
		}

		[Theory]
		[InlineData(SENDGRID_PUBLIC_KEY, SENDGRID_PAYLOAD, SENDGRID_SIGNATURE, SENDGRID_TIMESTAMP)]
		[InlineData(MY_PUBLIC_KEY, MY_PAYLOAD, MY_SIGNATURE, MY_TIMESTAMP)]
		[InlineData(STACKOVERFLOW_PUBLIC_KEY, STACKOVERFLOW_PAYLOAD, STACKOVERFLOW_SIGNATURE, STACKOVERFLOW_TIMESTAMP)]
		public void ValidateWebhookSignature(string publicKey, string payload, string signature, string timestamp)
		{
			// Arrange
			var parser = new WebhookParser();
			var headers = new Dictionary<string, string>
			{
				{ "X-Twilio-Email-Event-Webhook-Signature", signature },
				{ "X-Twilio-Email-Event-Webhook-Timestamp", timestamp }
			};

			// Act
			var result = parser.ParseSignedEventsWebhook(payload, headers, publicKey);

			// Assert
			result.ShouldNotBeNull();
			result.Length.ShouldBe(11); // The sample payload contains 11 events
		}

		private Stream GetStream(string responseContent)
		{
			var byteArray = Encoding.UTF8.GetBytes(responseContent);
			var stream = new MemoryStream(byteArray);
			return stream;
		}
	}
}
