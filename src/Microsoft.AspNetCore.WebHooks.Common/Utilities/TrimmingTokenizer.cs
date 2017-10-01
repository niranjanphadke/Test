// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebHooks.Utilities
{
    /// <summary>
    /// Splits a <see cref="string"/> or <see cref="StringSegment"/> into trimmed <see cref="StringSegment"/>s.
    /// </summary>
    public struct TrimmingTokenizer : IEnumerable<StringSegment>
    {
        private readonly StringTokenizer _tokenizer;

        /// <summary>
        /// Instantiates a new <see cref="TrimmingTokenizer"/> with given <paramref name="value"/>. Will split segments
        /// using <paramref name="separators"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to split and trim.</param>
        /// <param name="separators">The collection of separator <see cref="char"/>s controlling the split.</param>
        public TrimmingTokenizer(string value, params char[] separators)
        {
            _tokenizer = new StringTokenizer(value, separators);
        }

        /// <summary>
        /// Instantiates a new <see cref="TrimmingTokenizer"/> with given <paramref name="value"/>. Will split segments
        /// using <paramref name="separators"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to split and trim.</param>
        /// <param name="separators">The collection of separator <see cref="char"/>s controlling the split.</param>
        public TrimmingTokenizer(StringSegment value, params char[] separators)
        {
            _tokenizer = new StringTokenizer(value, separators);
        }

        /// <summary>
        /// Gets the number of elements in this <see cref="TrimmingTokenizer"/>.
        /// </summary>
        /// <remarks>
        /// Provided to avoid either (or both) <c>System.Linq</c> use or boxing the <see cref="TrimmingTokenizer"/>.
        /// </remarks>
        public int Count
        {
            get
            {
                var enumerator = GetEnumerator();
                var count = 0;
                while (enumerator.MoveNext())
                {
                    count++;
                }

                return count;
            }
        }

        /// <summary>
        /// Returns an <see cref="Enumerator"/> that iterates through the split and trimmed
        /// <see cref="StringSegment"/>s.
        /// </summary>
        /// <returns>
        /// An <see cref="Enumerator"/> that iterates through the split and trimmed <see cref="StringSegment"/>s.
        /// </returns>
        public Enumerator GetEnumerator() => new Enumerator(_tokenizer);

        /// <inheritdoc />
        IEnumerator<StringSegment> IEnumerable<StringSegment>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// An <see cref="IEnumerator{StringSegment}"/> wrapping <see cref="StringTokenizer.Enumerator"/> and providing
        /// trimmed <see cref="StringSegment"/>s.
        /// </summary>
        public struct Enumerator : IEnumerator<StringSegment>, IEnumerator, IDisposable
        {
            private readonly StringTokenizer.Enumerator _enumerator;

            // ??? Should we instead pass the TrimmingTokenizer? By ref? Current parameter does not match
            // ??? StringTokenizer.Enumerator's constructor. TrimmingTokenizer has only one backing field versus
            // ??? StringTokenizer's four. But, such a change may be a premature optimization.
            /// <summary>
            /// Instantiates a new <see cref="Enumerator"/> instance for <paramref name="tokenizer"/>.
            /// </summary>
            /// <param name="tokenizer">The underlying <see cref="StringTokenizer"/>.</param>
            public Enumerator(StringTokenizer tokenizer)
            {
                _enumerator = tokenizer.GetEnumerator();
            }

            /// <inheritdoc />
            public StringSegment Current => _enumerator.Current.Trim();

            /// <inheritdoc />
            object IEnumerator.Current => Current;

            /// <inheritdoc />
            public void Dispose() => _enumerator.Dispose();

            /// <inheritdoc />
            public bool MoveNext() => _enumerator.MoveNext();

            /// <inheritdoc />
            public void Reset() => _enumerator.Reset();
        }
    }
}
