// 
// Copyright (c) 2004-2018 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

namespace NLog.LayoutRenderers
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Text;
    using NLog.Config;

    /// <summary>
    /// Current date and time.
    /// </summary>
    [LayoutRenderer("date")]
    [ThreadAgnostic]
    [ThreadSafe]
    public class DateLayoutRenderer : LayoutRenderer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateLayoutRenderer" /> class.
        /// </summary>
        public DateLayoutRenderer()
        {
            Format = "yyyy/MM/dd HH:mm:ss.fff";
            Culture = CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Gets or sets the culture used for rendering. 
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// Gets or sets the date format. Can be any argument accepted by DateTime.ToString(format).
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        [DefaultParameter]
        public string Format
        {
            get => _format;
            set
            {
                _format = value;
                // Check if caching should be used
                if (IsLowTimeResolutionLayout(_format))
                    _cachedDateFormatted = new CachedDateFormatted(DateTime.MaxValue, string.Empty); // Cache can be used, will update cache-value
                else
                    _cachedDateFormatted = new CachedDateFormatted(DateTime.MinValue, string.Empty); // No cache support
            }
        }
        private string _format;

        private const string _lowTimeResolutionChars = "YyMDdHh";
        private CachedDateFormatted _cachedDateFormatted = new CachedDateFormatted(DateTime.MinValue, string.Empty);

        /// <summary>
        /// Gets or sets a value indicating whether to output UTC time instead of local time.
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        [DefaultValue(false)]
        public bool UniversalTime { get; set; }

        /// <summary>
        /// Renders the current date and appends it to the specified <see cref="StringBuilder" />.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to append the rendered data to.</param>
        /// <param name="logEvent">Logging event.</param>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var formatProvider = GetFormatProvider(logEvent, Culture);

            var timestamp = logEvent.TimeStamp;
            if (UniversalTime)
            {
                timestamp = timestamp.ToUniversalTime();
            }

            var cachedDateFormatted = _cachedDateFormatted;
            if (!ReferenceEquals(formatProvider, CultureInfo.InvariantCulture) || cachedDateFormatted.Date == DateTime.MinValue)
            {
                cachedDateFormatted = null;
            }
            else
            {
                if (cachedDateFormatted.Date == timestamp.Date.AddHours(timestamp.Hour))
                {
                    builder.Append(cachedDateFormatted.FormattedDate);
                    return; // Cache hit
                }
            }

            string formatTime = timestamp.ToString(_format, formatProvider);
            if (cachedDateFormatted != null)
            {
                _cachedDateFormatted = new CachedDateFormatted(timestamp.Date.AddHours(timestamp.Hour), formatTime);
            }
            builder.Append(formatTime);
        }

        private static bool IsLowTimeResolutionLayout(string dateTimeFormat)
        {
            for (int i = 0; i < dateTimeFormat.Length; ++i)
            {
                char ch = dateTimeFormat[i];
                if (char.IsLetter(ch) && _lowTimeResolutionChars.IndexOf(ch) < 0)
                    return false;
            }
            return true;
        }

        private class CachedDateFormatted
        {
            public CachedDateFormatted(DateTime date, string formattedDate)
            {
                Date = date;
                FormattedDate = formattedDate;
            }

            public readonly DateTime Date;
            public readonly string FormattedDate;
        }
    }
}
