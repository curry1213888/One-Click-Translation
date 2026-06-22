import sys
from PIL import Image, ImageDraw

def create_rounded_mask(size, radius):
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, size[0], size[1]), radius=radius, fill=255)
    return mask

def crop_transparent(img):
    bbox = img.getbbox()
    if bbox:
        return img.crop(bbox)
    return img

def main():
    img_path = r"C:\Users\28772\.cursor\projects\d-Desktop-ctrlTranslate\assets\ctrl-translator-logo.png"
    out_path = "CtrlTranslator.App/app.ico"
    
    img = Image.open(img_path).convert("RGBA")
    
    # Simple approach: If the image has white corners, we can crop the central squircle.
    # We don't know the exact bounding box, let's assume the user generated a standard square with a squircle in the middle.
    # Usually AI generators put a border around it. 
    # Let's try to remove white/solid background using flood fill from corners.
    
    width, height = img.size
    
    # Try flood fill transparency from 4 corners
    target_color = img.getpixel((0,0))
    # If the corner is white or black, make it transparent
    from PIL import ImageDraw
    ImageDraw.floodfill(img, (0, 0), (0, 0, 0, 0), thresh=20)
    ImageDraw.floodfill(img, (width-1, 0), (0, 0, 0, 0), thresh=20)
    ImageDraw.floodfill(img, (0, height-1), (0, 0, 0, 0), thresh=20)
    ImageDraw.floodfill(img, (width-1, height-1), (0, 0, 0, 0), thresh=20)
    
    # Crop to bounding box
    img = crop_transparent(img)
    
    # Resize to standard icon sizes
    sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)]
    
    # Save as ICO
    img.save(out_path, format="ICO", sizes=sizes)
    print("Saved as", out_path)

if __name__ == "__main__":
    main()