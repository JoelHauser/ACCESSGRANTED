"""
A synthetic fingerprint, made the way synthetic-fingerprint datasets are (SFinGe style):
an orientation field for a loop pattern (one core, one delta, Sherlock-Monro model), then
noise grown into ridges by repeated Gabor filtering along that field. No real person's
print is used. Writes a white PNG with the ridges in its alpha channel.
"""
import sys
import numpy as np
from PIL import Image

W, H = 440, 560
rng = np.random.default_rng(7)
y, x = np.mgrid[0:H, 0:W].astype(np.float64)

# Orientation field: half the difference of the angles to the core and to the delta.
core = complex(222, 230)
delta = complex(150, 430)
z = x + 1j * y
theta = 0.5 * (np.angle(z - core) - np.angle(z - delta))
# A gentle global tilt so the lower ridges run across the finger rather than down it.
theta += 0.12

# Ridge period in pixels.
period = 8.5
sigma = 3.2

def gabor(angle):
    k = int(3 * sigma) * 2 + 1
    h = k // 2
    gy, gx = np.mgrid[-h:h + 1, -h:h + 1].astype(np.float64)
    # Waves run across the ridge direction.
    xr = gx * np.cos(angle) + gy * np.sin(angle)
    yr = -gx * np.sin(angle) + gy * np.cos(angle)
    kernel = np.exp(-(xr ** 2 + yr ** 2) / (2 * sigma ** 2)) * np.cos(2 * np.pi * yr / period)
    return kernel - kernel.mean()

bins = 24
angles = (theta % np.pi)
index = np.floor(angles / np.pi * bins).astype(int) % bins
kernels = [gabor(b * np.pi / bins + np.pi / bins / 2) for b in range(bins)]
kernel_ffts = []
for kernel in kernels:
    padded = np.zeros((H, W))
    k = kernel.shape[0]
    padded[:k, :k] = kernel
    padded = np.roll(padded, (-(k // 2), -(k // 2)), axis=(0, 1))
    kernel_ffts.append(np.fft.rfft2(padded))

image = rng.standard_normal((H, W)) * (rng.random((H, W)) < 0.02)
for step in range(18):
    spectrum = np.fft.rfft2(image)
    out = np.zeros((H, W))
    for b in range(bins):
        mask = index == b
        if mask.any():
            out[mask] = np.fft.irfft2(spectrum * kernel_ffts[b], s=(H, W))[mask]
    out /= (np.abs(out).max() + 1e-9)
    image = np.tanh(out * 4.0)

# The fingertip: an oval, softly edged, a little narrower at the base.
cx, cy, rx, ry = W / 2, H / 2 - 10, W * 0.43, H * 0.46
taper = 1.0 - 0.12 * np.clip((y - cy) / ry, 0, 1)
d = ((x - cx) / (rx * taper)) ** 2 + ((y - cy) / ry) ** 2
edge = np.clip((1.0 - d) / 0.12, 0, 1)

ridges = np.clip(image, 0, 1)
# Pressure falls off toward the edge, as in a real impression, with a little grain.
strength = edge * (0.55 + 0.45 * np.clip(1.0 - d, 0, 1))
grain = 0.85 + 0.15 * rng.random((H, W))
alpha = np.clip(ridges * strength * grain, 0, 1)

# White, with the print in the alpha channel, so the terminal can tint it any colour.
a = (alpha * 255).astype(np.uint8)
rgba = np.dstack([np.full_like(a, 255)] * 3 + [a])
Image.fromarray(rgba, mode='RGBA').save(sys.argv[1], optimize=True)
print('saved', sys.argv[1])
