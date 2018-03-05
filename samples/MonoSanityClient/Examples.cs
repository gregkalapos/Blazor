// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using WebAssembly;
using System.Linq;
using System.Collections.Generic;

namespace MonoSanityClient
{
	public static class Examples
	{

		public static string GetWinnerDotNet()
		{
			var winner = MeetupAttendeeListFacebook
				.Concat(MeetupAttendeeListMeetupDotCom)
				.Distinct()
				.Except(AlreadyWon)
				.ElementAt((new Random()).Next(MeetupAttendeeListFacebook.Count + MeetupAttendeeListFacebook.Count - 1 - AlreadyWon.Count));

			AlreadyWon.Add(winner);
			return winner;
		}

		public static List<String> AlreadyWon = new List<String>();

		public static List<string> MeetupAttendeeListFacebook = new List<string>
		{
			"FbUser1",
			"FbUser2",
			"FbUser3"
		};

		public static List<string> MeetupAttendeeListMeetupDotCom = new List<string>
		{
			"MeetupUser1",
			"MeetupUser2",
			"MeetupUser3"
		};
		public static void TriggerException(string message)
		{
			throw new InvalidOperationException(message);
		}

		public static string EvaluateJavaScript(string expression)
		{
			// For tests that call this method, we'll exercise the 'InvokeJSArray' code path
			var result = Runtime.InvokeJSArray<string>(out var exceptionMessage, "evaluateJsExpression", expression, null, null);
			if (exceptionMessage != null)
			{
				return $".NET got exception: {exceptionMessage}";
			}

			return $".NET received: {(result ?? "(NULL)")}";
		}

		public static string CallJsNoBoxing(int numberA, int numberB)
		{
			// For tests that call this method, we'll exercise the 'InvokeJS' code path
			// since that doesn't box the params
			var result = Runtime.InvokeJS<int, int, object, int>(out var exceptionMessage, "divideNumbersUnmarshalled", numberA, numberB, null);
			if (exceptionMessage != null)
			{
				return $".NET got exception: {exceptionMessage}";
			}

			return $".NET received: {result}";
		}
	}
}
