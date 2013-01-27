// Copyright (c) 2006 Gratian Lup. All rights reserved.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
// * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 
// * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following
// disclaimer in the documentation and/or other materials provided
// with the distribution.
//
// * The name "DebugUtils" must not be used to endorse or promote 
// products derived from this software without prior written permission.
//
// * Products derived from this software may not be called "DebugUtils" nor 
// may "DebugUtils" appear in their names without prior written 
// permission of the author.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using LGratian.Debugger;
using LGratian.Debugger.Listeners;
using LGratian.Debugger.CrashNotifiers;
using LGratian.Debugger.IterationNotifiers;
using LGratian.Debugger.ObjectCounterNotifiers;
using LGratian.Debugger.DebugMessageNotifiers;

namespace LGratian.Debugger.UnitTests
{
	#region Helper objects

	internal class TestListener : DebugListenerBase
	{
		public int dumpCount = 0;

		public TestListener(int id)
		{
			ListnerId = id;
			_enabled = true;
		}

		public override bool Open()
		{
			return true;
		}

		public override bool Close()
		{
			return true;
		}

		public override bool DumpMessage(DebugMessage message)
		{
			dumpCount++;

			return true;
		}

		private bool _enabled;
		public override bool Enabled
		{
			get { return _enabled; }
			set { _enabled = value; }
		}
	}

	internal class TestFilter : DebugMessageFilterBase
	{
		public TestFilter(int id)
		{
			FilterId = id;
			Enabled = true;
		}

		public override bool FilterMessage(DebugMessage message)
		{
			if (message == null)
			{
				return true;
			}

			if (message.message.Contains("block"))
			{
				return true;
			}

			return false;
		}
	}

	#endregion

	[TestFixture]
	public class UnitTest
	{
		[SetUp]
		public void TestSetup()
		{
			Debug.Enabled = true;
			Debug.StoreMessages = true;
			Debug.BreakOnFailedAssertion = false;
		}


		[Test]
		public void TestAssert()
		{
			int count = Debug.StoredMessageCount;
			Debug.StoreMessages = true;

			Debug.Assert(false);
			Assert.IsTrue(Debug.StoredMessageCount == count + 1, "Failed to store Assert message");

			count = Debug.StoredMessageCount;
			Debug.AssertNotNull(null);
			Assert.IsTrue(Debug.StoredMessageCount == count + 1, "Failed to store AssertNull message");

			count = Debug.StoredMessageCount;
			Debug.AssertType(25, typeof(char));
			Assert.IsTrue(Debug.StoredMessageCount == count + 1, "Failed to store AssertType message");

			count = Debug.StoredMessageCount;
			Debug.Report("Test {0}", "1234");
			Assert.IsTrue(Debug.StoredMessageCount == count + 1, "Failed to store Report message");

			count = Debug.StoredMessageCount;
			Debug.ReportWarning("Test {0}", "1234");
			Assert.IsTrue(Debug.StoredMessageCount == count + 1, "Failed to store ReportWarning message");

			count = Debug.StoredMessageCount;
			Debug.ReportError("Test {0}", "1234");
			Assert.IsTrue(Debug.StoredMessageCount == count + 1, "Failed to store ReportError message");

			// remove all stored messages
			Debug.ClearMessageStore();
			Assert.IsTrue(Debug.StoredMessageCount == 0, "Failed to clear message store");
		}


		[Test]
		public void TestListner()
		{
			// add the listener
			TestListener testListnerOne = new TestListener(1234);
			TestListener testListnerTwo = new TestListener(12345);

			// remove previous listeners
			Debug.RemoveAllListeners();
			Assert.AreEqual(Debug.ListenerCount, 0, "Failed to remove all listeners");

			// add listeners
			Debug.AddListner(testListnerOne);
			Debug.AddListner(testListnerTwo);
			Assert.AreEqual(Debug.ListenerCount,2, "Failed to add listeners");

			// add duplicate listener
			Assert.IsFalse(Debug.AddListner(testListnerOne), "Added duplicate test listener");

			// get listener by id
			Assert.IsNotNull(Debug.GetListnerById(1234), "Failed to get listener by ID");

			// get listener by index
			Assert.IsNotNull(Debug.GetListnerByIndex(1), "Failed to get listener by index");

			// get invalid listener by id
			Assert.IsNull(Debug.GetListnerById(567), "Returned invalid listener by ID");

			// get invalid listener by index
			Assert.IsNull(Debug.GetListnerByIndex(2), "Returned invalid listener by index");

			testListnerOne.dumpCount = 0;
			testListnerOne.dumpCount = 0;

			testListnerTwo.Enabled = false;

			// dump some messages
			Debug.RemoveAllFilters();

			Debug.Report("Test string");
			Debug.Report("Test string");
			Debug.Report("Test string");

			Assert.AreEqual(3,testListnerOne.dumpCount, "Failed to send messages to listener");
			Assert.AreEqual(testListnerTwo.dumpCount, 0, "Messages sent to disabled listener");
			
			// remove the listeners
			Debug.RemoveAllListeners();
			Assert.IsTrue(Debug.ListenerCount == 0, "Failed to remove all listeners");
		}


		[Test]
		public void TestFilter()
		{
			TestFilter filter = new TestFilter(1234);
			TestListener listener = new TestListener(12345);

			// remove previous filters
			Debug.RemoveAllFilters();
			Assert.AreEqual(Debug.FilterCount, 0, "Failed to remove all filters");

			// add the filter
			Debug.AddFilter(filter);
			Assert.AreEqual(Debug.FilterCount, 1, "Failed to add filter");

			// add duplicate filter
			Assert.IsFalse(Debug.AddFilter(filter), "Added duplicate filter");

			// get filter by ID
			Assert.IsNotNull(Debug.GetFilterById(1234), "Failed to get filter by ID");

			// get filter by index
			Assert.IsNotNull(Debug.GetFilterByIndex(0), "Failed to get filter by index");

			// get invalid listener by id
			Assert.IsNull(Debug.GetFilterById(567), "Returned invalid listener by ID");

			// get invalid listener by index
			Assert.IsNull(Debug.GetFilterByIndex(1), "Returned invalid listener by index");

			// add the listener
			Debug.AddListner(listener);
			listener.dumpCount = 0;

			// dump some messages
			Debug.Report("test");
			Debug.Report("block test");
			
			Assert.AreNotEqual(0,listener.dumpCount, "All messages blocked");
			Assert.LessOrEqual(1,listener.dumpCount, "Some messages not blocked");

			// test with the filter disabled
			filter.Enabled = false;

			listener.dumpCount = 0;
			Debug.Report("test");
			Debug.Report("block test");

			Assert.AreNotEqual(0,listener.dumpCount, "All messages blocked");
			Assert.AreEqual(2,listener.dumpCount, "Some messages not blocked");

			// remove all filters
			Debug.RemoveAllFilters();
			Assert.AreEqual(0,Debug.FilterCount, "Failed to remove all listeners");
		}


		[Test]
		public void OtherDebuggerTests()
		{
			// test BreakOnFailedAssertion
			Debug.BreakOnFailedAssertion = true;
			bool exceptionThrown = false;

			try
			{
				Debug.Assert(false);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				exceptionThrown = true;
			}

			Assert.IsTrue(exceptionThrown, "Failed to break on assertion");
		}


		[Test]
		[DebugOptions(Debug = true,Store = true)]
		public void DebugOptionsAttributeTest()
		{
			Debug.Enabled = false;
			Debug.StoreMessages = false;

			int count = Debug.StoredMessageCount;
			Debug.Assert(false);
			Assert.AreEqual(count + 1, Debug.StoredMessageCount,"Failed to interpret debug options attribute");
		}
	}
}
