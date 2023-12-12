
#ifndef RANDOM_INCLUDE
#define RANDOM_INCLUDE

// Wang Hash Random
#define WANG_HASH_SEED_MAX 4294967295
#define INV_WANG_HASH_DIV 2.3283064e-10
float wang_hash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed * INV_WANG_HASH_DIV;
}

float random(float t)
{
    return frac(sin(t * 12345.564) * 7658.76);
}

uint pcg(uint v)
{
    uint state = v * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (word >> 22u) ^ word;
}
#endif // RANDOM_INCLUDE