﻿using System;
using System.Collections.Generic;

namespace AzureBusDepot.Abstractions
{
    public class VoidInstrumentor : IInstrumentor
    {
        public void TrackElapsed(
            int eventId,
            long elapsedMilliseconds,
            string name = "",
            IDictionary<string, object> customProperties = null)
        {
        }

        public void TrackRequest(
            int eventId,
            long elapsedMilliseconds,
            DateTimeOffset timestamp,
            string name,
            string source = null,
            Uri uri = null,
            bool isSuccessful = true,
            IDictionary<string, object> customProperties = null)
        {
        }

        public void TrackDependency(
            int eventId,
            long elapsedMilliseconds,
            DateTimeOffset timestamp,
            string type,
            string target,
            string name,
            string data = null,
            bool isSuccessful = true,
            IDictionary<string, object> customProperties = null)
        {
        }
    }
}