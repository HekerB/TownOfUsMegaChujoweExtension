using System;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Modules;

/// <summary>
///     Random source used by every draft decision (pool order, offers, pick order, slot buckets).
/// </summary>
public interface IDraftRng
{
    /// <summary>Uniform int in [0, maxExclusive).</summary>
    int NextInt(int maxExclusive);

    /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
    int NextInt(int minInclusive, int maxExclusive);

    /// <summary>Uniform double in [0, 1).</summary>
    double NextDouble();
}

/// <summary>Live randomness - what the draft uses in a real lobby.</summary>
/// <remarks>
/// A generator of the draft's own rather than <c>UnityEngine.Random</c>. That one is a single global
/// sequence the whole game shares: the base game re-seeds it at points of its own choosing, and every
/// other system in the process draws from it in between. Two drafts in one lobby could therefore start
/// from near enough the same place and offer near enough the same roles, which is what "it keeps giving
/// out the same roles" was. Nothing outside this class can touch this sequence or move it along.
/// <para>
/// xoshiro128** for the sequence and splitmix32 to spread the seed into its four words - a well-known
/// pair, cheap, and with none of the short-cycle behaviour a hand-rolled generator tends to have.
/// </para>
/// </remarks>
public sealed class UnityDraftRng : IDraftRng
{
    private uint _s0;
    private uint _s1;
    private uint _s2;
    private uint _s3;

    public UnityDraftRng()
        : this(MakeSeed())
    {
    }

    public UnityDraftRng(uint seed)
    {
        // splitmix32: one seed number in, four well-spread state words out. Seeding the state directly
        // from the number leaves a generator that needs thousands of draws to shake off a poor seed.
        var z = seed == 0u ? 0x9E3779B9u : seed;
        _s0 = SplitMix(ref z);
        _s1 = SplitMix(ref z);
        _s2 = SplitMix(ref z);
        _s3 = SplitMix(ref z);

        if ((_s0 | _s1 | _s2 | _s3) == 0u)
        {
            _s0 = 0x9E3779B9u;
        }
    }

    /// <summary>Builds a seed that differs between drafts, lobbies and machines.</summary>
    private static uint MakeSeed()
    {
        unchecked
        {
            var seed = (uint)Environment.TickCount;
            seed ^= (uint)Guid.NewGuid().GetHashCode();
            seed ^= (uint)DateTime.UtcNow.Ticks;
            return seed;
        }
    }

    private static uint SplitMix(ref uint state)
    {
        unchecked
        {
            state += 0x9E3779B9u;
            var z = state;
            z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
            z = (z ^ (z >> 13)) * 0xC2B2AE35u;
            return z ^ (z >> 16);
        }
    }

    private static uint RotateLeft(uint x, int k)
    {
        return (x << k) | (x >> (32 - k));
    }

    private uint NextState()
    {
        unchecked
        {
            var result = RotateLeft(_s1 * 5u, 7) * 9u;
            var t = _s1 << 9;

            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = RotateLeft(_s3, 11);

            return result;
        }
    }

    public int NextInt(int maxExclusive)
    {
        return DraftRngMath.Bounded(NextState, maxExclusive);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        return maxExclusive <= minInclusive
            ? minInclusive
            : minInclusive + DraftRngMath.Bounded(NextState, maxExclusive - minInclusive);
    }

    public double NextDouble()
    {
        return (NextState() >> 8) / (double)(1u << 24);
    }
}

/// <summary>Shared by both generators: an unbiased number below a bound.</summary>
internal static class DraftRngMath
{
    /// <summary>
    /// A uniform value in [0, bound), with the bias that plain <c>% bound</c> leaves taken out.
    /// </summary>
    /// <remarks>
    /// Modulo alone favours the low end of the range whenever the bound does not divide 2^32 - with
    /// three cards on the table the first is drawn slightly more often than the third, forever. The
    /// rejection loop below removes that; it runs more than once about as often as a coin lands on its
    /// edge.
    /// </remarks>
    /// <param name="next">The generator's raw word source.</param>
    /// <param name="bound">Exclusive upper bound.</param>
    /// <returns>A value in [0, bound), or 0 when the bound is not positive.</returns>
    internal static int Bounded(Func<uint> next, int bound)
    {
        if (bound <= 0)
        {
            return 0;
        }

        var limit = (uint)bound;
        var threshold = (uint)(-(int)limit) % limit;

        while (true)
        {
            var value = next();
            if (value >= threshold)
            {
                return (int)(value % limit);
            }
        }
    }
}

/// <summary>
///     xorshift32 seeded from a single number, so a draft can be replayed exactly. Used when a seed
///     has been pinned via <see cref="DraftRng.UseSeed" /> - handy when chasing a "why did this player
///     get offered that" report, because the whole draft becomes reproducible from the logged seed.
/// </summary>
public sealed class DeterministicDraftRng : IDraftRng
{
    private uint _state;

    public DeterministicDraftRng(uint seed)
    {
        _state = seed == 0u ? 0x9E3779B9u : seed;
    }

    private uint NextState()
    {
        _state ^= _state << 13;
        _state ^= _state >> 17;
        _state ^= _state << 5;
        return _state;
    }

    public int NextInt(int maxExclusive)
    {
        return DraftRngMath.Bounded(NextState, maxExclusive);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        return maxExclusive <= minInclusive
            ? minInclusive
            : minInclusive + DraftRngMath.Bounded(NextState, maxExclusive - minInclusive);
    }

    public double NextDouble()
    {
        return (NextState() >> 8) / (double)(1u << 24);
    }
}

/// <summary>
///     Facade the draft code calls instead of <c>UnityEngine.Random</c> directly. Keeping every draft
///     roll behind one object is what makes a draft reproducible; call <see cref="UseSeed" /> before a
///     draft starts and the exact same offers come back out.
/// </summary>
public static class DraftRng
{
    private static IDraftRng _source = new UnityDraftRng();

    /// <summary>The seed currently pinned, or null when running on live randomness.</summary>
    public static uint? Seed { get; private set; }

    /// <summary>Pins a seed, making every subsequent draft roll reproducible.</summary>
    public static void UseSeed(uint seed)
    {
        Seed = seed;
        _source = new DeterministicDraftRng(seed);
    }

    /// <summary>Goes back to live randomness.</summary>
    public static void UseLiveRandomness()
    {
        Seed = null;
        _source = new UnityDraftRng();
    }

    /// <summary>
    ///     Re-primes the generator for a new draft. With a pinned seed the sequence restarts from it,
    ///     so two drafts in the same lobby produce identical results.
    /// </summary>
    public static void ResetForNewDraft()
    {
        if (Seed.HasValue)
        {
            _source = new DeterministicDraftRng(Seed.Value);
            return;
        }

        // A new sequence per draft. Carrying one generator across a whole session was survivable, but
        // starting each draft afresh means one bad run cannot colour the next.
        _source = new UnityDraftRng();
    }

    /// <summary>Matches <c>UnityEngine.Random.Range(int, int)</c>: max is exclusive.</summary>
    public static int Range(int minInclusive, int maxExclusive)
    {
        return _source.NextInt(minInclusive, maxExclusive);
    }

    /// <summary>Matches <c>UnityEngine.Random.Range(float, float)</c>: max is inclusive.</summary>
    public static float Range(float minInclusive, float maxInclusive)
    {
        if (maxInclusive <= minInclusive)
        {
            return minInclusive;
        }

        return minInclusive + ((float)_source.NextDouble() * (maxInclusive - minInclusive));
    }

    /// <summary>Uniform float in [0, 1).</summary>
    public static float Value => (float)_source.NextDouble();

    /// <summary>In-place Fisher-Yates using the draft's own source, so shuffles are reproducible too.</summary>
    public static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _source.NextInt(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
