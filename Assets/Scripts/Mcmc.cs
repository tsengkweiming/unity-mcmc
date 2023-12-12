using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Mcmc
{
	[System.Serializable]
	public class NoiseProb
    {
		public Vector2Int TexSize;
		public float[] Values;
		public Color[] Pixels;
	}

    public class MCMC
    {
		public const int LIMIT_RESET_LOOP_COUNT = 100;
		public Texture2D ProbTex { get; private set; }
		public float StdDev { get; private set; }
		public float Aspect { get; private set; }
		public float Height { get; private set; }
		public float Epsilon { get; private set; }

		public NoiseProb NoiseProb { get; private set; }
		public float[] Prob { get; private set; }
		public int   TexWidth { get; private set; }
		public int   TexHeight { get; private set; }

		private Vector2 _curr;
		private float _currDensity = 0f;
		private Vector2 _stddevAspect;

		public MCMC(Texture2D probTex, float stddev) : this(probTex, stddev, 1f) { }
		public MCMC(Texture2D probTex, float stddev, float height)
		{
			this.ProbTex = probTex;
			this.Height = height;
			this.StdDev = stddev;
            this.TexWidth = probTex.width;
            this.TexHeight = probTex.height;

        }

		public MCMC(NoiseProb prob, float stddev, float height, float epsilon)
		{
			this.NoiseProb = prob;
			this.Height = height;
			this.StdDev = stddev;
			this.Epsilon = epsilon;
			this.TexWidth = prob.TexSize.x;
			this.TexHeight = prob.TexSize.y;
		}

		public void ResetTex()
		{
			for (var i = 0; _currDensity <= 0f && i < LIMIT_RESET_LOOP_COUNT; i++)
			{
				_curr = new Vector2(Random.value, Random.value);
				_currDensity = DensityTex(_curr);
			}
			Aspect = (float)ProbTex.width / ProbTex.height;
			_stddevAspect = new Vector2(StdDev, StdDev / Aspect);
		}

		public IEnumerable<Vector2> SequenceTex(int nInitialize, int limit)
		{
			return SequenceTex(nInitialize, limit, 0);
		}
		public IEnumerable<Vector2> SequenceTex(int nInitialize, int limit, int nSkip)
		{
			ResetTex();

			for (var i = 0; i < nInitialize; i++)
				NextTex();

			for (var i = 0; i < limit; i++)
			{
				for (var j = 0; j < nSkip; j++)
					NextTex();
				yield return _curr;
				NextTex();
			}
		}

		void NextTex()
		{
			var next = Vector2.Scale(_stddevAspect, BoxMuller.Gaussian()) + _curr;
			next.x -= Mathf.Floor(next.x);
			next.y -= Mathf.Floor(next.y);

			var densityNext = DensityTex(next);
			if (_currDensity <= 0f || Mathf.Min(1f, densityNext / _currDensity) >= Random.value)
			{
				_curr = next;
				_currDensity = densityNext;
			}
		}

		float DensityTex(Vector2 curr)
		{
			return Height * ProbTex.GetPixelBilinear(curr.x, curr.y).r + Epsilon;
		}

		public IEnumerable<Vector2> Sequence(int nInitialize, int limit)
		{
			return Sequence(nInitialize, limit, 0);
		}
		public IEnumerable<Vector2> Sequence(int nInitialize, int limit, int nSkip)
		{
			Reset();

			for (var i = 0; i < nInitialize; i++)
				Next();

			for (var i = 0; i < limit; i++)
			{
				for (var j = 0; j < nSkip; j++)
					Next();
				yield return _curr;
				Next();
			}
		}

		public void Reset()
		{
			for (var i = 0; _currDensity <= 0f && i < LIMIT_RESET_LOOP_COUNT; i++)
			{
				_curr = new Vector2(Random.value, Random.value);
				_currDensity = Density(_curr);
			}
			Aspect = (float)TexWidth / TexHeight;
			_stddevAspect = new Vector2(StdDev, StdDev / Aspect);
		}

		void Next()
		{
			var next = Vector2.Scale(_stddevAspect, BoxMuller.Gaussian()) + _curr;
			next.x -= Mathf.Floor(next.x);
			next.y -= Mathf.Floor(next.y);

			var densityNext = Density(next);
			if (_currDensity <= 0f || Mathf.Min(1f, densityNext / _currDensity) >= Random.value)
			{
				_curr = next;
				_currDensity = densityNext;
			}
		}

		float Density(Vector2 curr)
		{
			return Height * GetTextureValue(curr) + Epsilon;
		}

		public virtual float GetTextureValue(Vector2 uv)
		{
			return BilinearOne(uv, TexWidth, TexHeight);
		}

		public virtual float GetTextureValueFunc(int ix, int iy)
		{
			return NoiseProb.Values[ix + iy * TexWidth];
		}

		public float BilinearOne(Vector2 uv, int width, int height)
		{
			var lwidth  = width - 1;
			var lheight = height - 1;
			var x = uv.x * lwidth;
			var y = uv.y * lheight;

			var ix = (int)x;
			var iy = (int)y;
			ix = (ix < 0 ? 0 : (ix <= lwidth  ? ix : lwidth));
			iy = (iy < 0 ? 0 : (iy <= lheight ? iy : lheight));

			var jx = ix + 1;
			var jy = iy + 1;
			jx = (jx <= lwidth  ? jx : lwidth);
			jy = (jy <= lheight ? jy : lheight);

			var dx = x - ix;
			var dy = y - iy;

			return (1f - dx) * ((1f - dy) * GetTextureValueFunc(ix, iy) + dy * GetTextureValueFunc(ix, jy))
				+ dx * ((1f - dy) * GetTextureValueFunc(jx, iy) + dy * GetTextureValueFunc(jx, jy));
		}
	}
}