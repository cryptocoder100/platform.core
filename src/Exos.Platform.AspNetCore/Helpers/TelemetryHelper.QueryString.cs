#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable SA1601 // Partial elements should be documented

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Exos.Platform.AspNetCore.Helpers
{
    public static partial class TelemetryHelper
    {
        // PERF: Based on QueryStringEnumerable, but using StringSegments instead of ReadOnlyMemory<char>.
        // With StringSegment we can get absolute positions which is important later when we want to copy segments
        // of the original string and include delimiters which the QueryStringEnumerable doesn't provide us.
        // See: https://github.com/dotnet/aspnetcore/blob/v6.0.0/src/Shared/QueryStringEnumerable.cs

        private readonly struct QueryStringEnumerable
        {
            private readonly StringSegment _queryString;

            public QueryStringEnumerable(StringSegment queryString)
            {
                Debug.Assert(queryString.HasValue && queryString.Length > 0, $"Upstream caller must check '{nameof(queryString)}' for null or empty.");

                _queryString = queryString;
            }

            public Enumerator GetEnumerator()
                => new Enumerator(_queryString);

            public readonly struct EncodedNameValuePair
            {
                private readonly StringSegment _encodedName;
                private readonly StringSegment _encodedValue;

                public EncodedNameValuePair(StringSegment encodedName, StringSegment encodedValue)
                {
                    _encodedName = encodedName;
                    _encodedValue = encodedValue;
                }

                public readonly StringSegment EncodedName => _encodedName;
                public readonly StringSegment EncodedValue => _encodedValue;
            }

            public struct Enumerator
            {
                private EncodedNameValuePair _current;
                private StringSegment _queryString;
                private int _pos;

                public Enumerator(StringSegment queryString)
                {
                    Debug.Assert(queryString.HasValue && queryString.Length > 0, $"Upstream caller must check '{nameof(queryString)}' for null or empty.");

                    _queryString = queryString;
                    _current = default;
                    _pos = 0;

                    if (_queryString[0] == '?')
                    {
                        _pos++;
                    }
                }

                public EncodedNameValuePair Current => _current;

                public bool MoveNext()
                {
                    // PERF: Uses IndexOf throughout, which is vectorized
                    while (_pos < _queryString.Length)
                    {
                        // Get the next pair
                        StringSegment pair;
                        var delimiterIndex = _queryString.IndexOf('&', _pos);
                        if (delimiterIndex >= 0)
                        {
                            pair = _queryString.Subsegment(_pos, delimiterIndex - _pos);
                            _pos = delimiterIndex + 1;
                        }
                        else
                        {
                            pair = _queryString.Subsegment(_pos, _queryString.Length - _pos);
                            _pos = _queryString.Length; // End of stream
                        }

                        // Split the segment into name/value
                        var equalsIndex = pair.IndexOf('=');
                        if (equalsIndex >= 0)
                        {
                            _current = new EncodedNameValuePair(
                                pair.Subsegment(0, equalsIndex),
                                pair.Subsegment(equalsIndex + 1, pair.Length - equalsIndex - 1));

                            return true;
                        }
                        else if (pair.Length > 0)
                        {
                            // There is no value; only name
                            _current = new EncodedNameValuePair(
                                pair,
                                pair.Subsegment(0, 0));

                            return true;
                        }
                    }

                    return false;
                }
            }
        }
    }
}
