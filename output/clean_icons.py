import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent / 'icon-cleanup-tools'))
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

out = Path(__file__).parent / 'feedback-icons' / 'cleaned'
out.mkdir(parents=True, exist_ok=True)
names = {20: 'score', 19: 'distance', 18: 'vertical-tilt', 17: 'speed', 16: 'horizontal-tilt'}
previews = []
for number, name in names.items():
    source = Image.open(Path('C:/Users/user/Downloads') / f'image ({number}).png').convert('RGBA')
    a = np.array(source)
    mask = (a[:, :, 3] > 180) & (a[:, :, :3].min(axis=2) > 180)
    mask = ndi.binary_opening(mask, iterations=2)
    labels, count = ndi.label(mask)
    areas = np.bincount(labels.ravel())
    keep = areas > 150
    keep[0] = False
    mask = keep[labels]
    # Smooth the alpha edge without changing the large-scale geometry.
    alpha = np.clip((ndi.gaussian_filter(mask.astype(float), 0.65) - 0.08) / 0.84, 0, 1)
    rgba = np.empty_like(a)
    rgba[:, :, :3] = (242, 246, 250)
    rgba[:, :, 3] = np.round(alpha * 255).astype(np.uint8)
    rgba[rgba[:, :, 3] == 0, :3] = 0
    result = Image.fromarray(rgba)
    result.save(out / f'{name}.png')
    tile = Image.new('RGBA', (360, 400), '#242830')
    thumb = result.copy()
    thumb.thumbnail((340, 340))
    tile.alpha_composite(thumb, ((360-thumb.width)//2, 25))
    ImageDraw.Draw(tile).text((15, 375), name, fill='white')
    previews.append(tile)
    print(name, 'remaining components:', sum(keep))

sheet = Image.new('RGBA', (360*5, 400), '#242830')
for i, tile in enumerate(previews):
    sheet.alpha_composite(tile, (i*360, 0))
sheet.convert('RGB').save(out / 'preview.jpg')
