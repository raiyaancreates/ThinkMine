# Personal Website

A minimal personal website to showcase ThinkMine and other projects.

## Setup

### Local Development
1. Open `index.html` in your browser
2. Or use a local server: `python -m http.server 8000`

### Deploy to GitHub Pages

1. **Create a GitHub repository**
   ```bash
   git init
   git add .
   git commit -m "Initial commit"
   git branch -M main
   git remote add origin https://github.com/yourusername/yourusername.github.io.git
   git push -u origin main
   ```

2. **Enable GitHub Pages**
   - Go to repository Settings → Pages
   - Source: Deploy from branch `main`
   - Folder: `/ (root)`
   - Save

3. **Your site will be live at**: `https://yourusername.github.io`

### Custom Domain (Optional)

1. **Buy a domain** from Namecheap (~$10/year)

2. **Add DNS records** in Namecheap:
   - Type: `A`, Host: `@`, Value: `185.199.108.153`
   - Type: `A`, Host: `@`, Value: `185.199.109.153`
   - Type: `A`, Host: `@`, Value: `185.199.110.153`
   - Type: `A`, Host: `@`, Value: `185.199.111.153`
   - Type: `CNAME`, Host: `www`, Value: `yourusername.github.io`

3. **Add custom domain in GitHub**:
   - Settings → Pages → Custom domain
   - Enter your domain (e.g., `raiyaan.com`)
   - Enable "Enforce HTTPS"

4. **Wait 24-48 hours** for DNS propagation

## Email Collection

To collect emails, you can use:
- **Buttondown** (free tier, simple)
- **ConvertKit** (free up to 1000 subscribers)
- **Mailchimp** (free up to 2000 subscribers)

Update the form action in `script.js` with your chosen service's endpoint.

## Analytics

Add Google Analytics or Plausible to track:
- Page views
- Download clicks
- Email signups

## Customization

- Update `index.html` with your name and story
- Replace download links in `thinkmine/index.html`
- Add your GitHub username in README
- Customize colors in `style.css`
