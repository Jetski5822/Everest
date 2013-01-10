﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Everest.Pipeline;
using Everest.Status;
using NUnit.Framework;
using SelfishHttp;

namespace Everest.UnitTests
{
    [TestFixture]
    public class EventsTest
    {
        private const string BaseAddress = "http://localhost:18747";
        private Server _server;

        [SetUp]
        public void StartServer()
        {
            _server = new Server(18747);
        }

        [TearDown]
        public void StopServer()
        {
            _server.Stop();
        }

        [Test]
        public void RaisesSendingEventForEachRequest()
        {
            _server.OnGet("/foo").RespondWith("awww yeah");

            var client = new RestClient();
            var sendingEvents = new List<RequestDetails>();
            client.Sending += (sender, args) => sendingEvents.Add(args.Request);
            client.Get(BaseAddress + "/foo?omg=yeah");
            Assert.That(sendingEvents.Single().RequestUri.PathAndQuery, Is.EqualTo("/foo?omg=yeah"));
            Assert.That(sendingEvents.Single().Method, Is.EqualTo("GET"));
            client.Get(BaseAddress + "/foo?omg=nah");
            Assert.That(sendingEvents.Skip(1).Single().RequestUri.PathAndQuery, Is.EqualTo("/foo?omg=nah"));
            Assert.That(sendingEvents.Skip(1).Single().Method, Is.EqualTo("GET"));
        }

        [Test]
        public void StillRaisesSendingEventWhenSendingThrows()
        {
            var sendingEvents = new List<RequestDetails>();

            var client = new RestClient(null, new AlwaysThrowsOnSendingAdapter(), new List<PipelineOption>());
            client.Sending += (sender, args) => sendingEvents.Add(args.Request);
            Assert.That(() => client.Get(BaseAddress + "/foo?omg=yeah"), Throws.Exception);
            Assert.That(sendingEvents.Single().RequestUri.PathAndQuery, Is.EqualTo("/foo?omg=yeah"));
            Assert.That(sendingEvents.Single().Method, Is.EqualTo("GET"));
        }

        [Test]
        public void RaisesRespondedEventForEachRequest()
        {
            _server.OnGet("/foo").Respond((req, res) => res.StatusCode = 418);
            var client = new RestClient();
            var respondedEvents = new List<ResponseDetails>();
            client.Responded += (sender, args) => respondedEvents.Add(args.Response);
            client.Get(BaseAddress + "/foo?teapot=yes", new ExpectStatus((HttpStatusCode)418));
            Assert.That(respondedEvents.Single().Status, Is.EqualTo(418));
            Assert.That(respondedEvents.Single().Request.RequestUri.PathAndQuery, Is.EqualTo("/foo?teapot=yes"));
        }

        [Test]
        public void DoesNotRaiseRespondedEventForRequestsWhenSendingThrows()
        {
            _server.OnGet("/foo").Respond((req, res) => res.StatusCode = 418);
            var client = new RestClient(null, new AlwaysThrowsOnSendingAdapter(), new List<PipelineOption>());
            var respondedEvents = new List<ResponseDetails>();
            client.Responded += (sender, args) => respondedEvents.Add(args.Response);
            Assert.That(() => client.Get(BaseAddress + "/foo?omg=yeah"), Throws.Exception);
            Assert.That(respondedEvents, Is.Empty);
        }

        [Test]
        public void RaisesExceptionEventForRequestsWhenSendingThrows()
        {
            var client = new RestClient(null, new AlwaysThrowsOnSendingAdapter(), new List<PipelineOption>());
            var sendErrors = new List<Exception>();
            client.SendError += (sender, args) => sendErrors.Add(args.Exception);
            Assert.That(() => client.Get("http://irrelevant"), Throws.Exception);
            Assert.That(sendErrors.Count, Is.EqualTo(1));
            Assert.That(sendErrors[0], Is.Not.InstanceOf<AggregateException>());
        }
    }
}