# 🚀 Deployment Guide for Entra Token CLI Documentation

This guide will help you deploy the documentation to the separate `entratool-docs` repository at https://github.com/garrardkitchen/entratool-docs

## ✅ What's Been Done

1. **Home page redesigned** with Lotus Docs blocks/sections layout
2. **Navigation menu configured** in `hugo.yaml` for all sections
3. **Missing sections created**: OAuth Flows and Certificates
4. **Documentation is complete** with 20+ comprehensive pages
5. **GitHub Actions workflow** ready for deployment

## 📋 Deployment Steps

### Step 1: Create the Documentation Repository

1. Go to https://github.com/new
2. Repository name: `entratool-docs`
3. Description: "Documentation for Entra Token CLI"
4. Public repository
5. **Do NOT** initialize with README, .gitignore, or license
6. Click "Create repository"

### Step 2: Enable GitHub Pages

1. Go to repository Settings → Pages
2. Source: **GitHub Actions** (not "Deploy from a branch")
3. Save

### Step 3: Push Documentation to New Repository

```bash
# Navigate to the docs directory
cd /Users/kitcheng/source/dotnet/access-token/docs/learn

# Initialize as git repository
git init

# Add all files
git add .

# Initial commit
git commit -m "Initial documentation site with Hugo and Lotus Docs"

# Add remote
git remote add origin https://github.com/garrardkitchen/entratool-docs.git

# Create main branch and push
git branch -M main
git push -u origin main
```

### Step 4: Create GitHub Actions Workflow

In the `entratool-docs` repository, create the workflow file:

```bash
# Create workflow directory
mkdir -p .github/workflows

# Copy the workflow file
cp DEPLOY_WORKFLOW.yml .github/workflows/hugo.yml

# Commit and push
git add .github/workflows/hugo.yml
git commit -m "Add GitHub Actions deployment workflow"
git push
```

Or create `.github/workflows/hugo.yml` directly in GitHub web interface with the content from `DEPLOY_WORKFLOW.yml`.

### Step 5: Configure Custom Domain (DNS)

Add a CNAME record in your DNS provider:

```
Type: CNAME
Name: entratool-docs
Value: garrardkitchen.github.io
TTL: 3600 (or default)
```

### Step 6: Configure Custom Domain in GitHub

1. Go to repository Settings → Pages
2. Custom domain: `entratool-docs.garrardkitchen.com`
3. Check "Enforce HTTPS" (after DNS propagates)
4. Save

## 🧪 Testing Locally

Before deploying, test the site locally:

```bash
cd /Users/kitcheng/source/dotnet/access-token/docs/learn

# Download Hugo modules
hugo mod get

# Start development server
hugo server -D

# Open http://localhost:1313
```

**What to check:**
- ✅ Home page renders with hero sections and feature cards
- ✅ All navigation links work
- ✅ Recipes, Reference, Platform Guides, and Troubleshooting pages display content
- ✅ Code blocks have syntax highlighting
- ✅ Internal links work correctly

## 📁 Files Created/Modified

### Modified Files
- `content/_index.md` - New hero-based home page with Lotus Docs blocks
- `hugo.yaml` - Added docs menu configuration for sidebar navigation

### New Files
- `content/docs/oauth-flows/_index.md` - OAuth2 flows section
- `content/docs/certificates/_index.md` - Certificate authentication guide
- `README.md` - Documentation maintenance guide
- `DEPLOY_WORKFLOW.yml` - GitHub Actions workflow for deployment

### Existing Complete Sections
- ✅ Getting Started (4 pages)
- ✅ Core Concepts (4 pages)
- ✅ User Guide (4 pages)
- ✅ Recipes (1 comprehensive page)
- ✅ Reference (1 comprehensive page)
- ✅ Platform Guides (1 comprehensive page)
- ✅ Troubleshooting (1 comprehensive page)
- ✅ OAuth Flows (1 overview page)
- ✅ Certificates (1 comprehensive guide)

## 🔍 Verification

After deployment, verify:

1. **Homepage**: https://entratool-docs.garrardkitchen.com/
   - Hero section displays
   - Feature cards render
   - Platform support badges show
   - Quick start code block visible

2. **Navigation**:
   - All menu items in sidebar work
   - Getting Started sections expand/collapse
   - All referenced pages load

3. **Content**:
   - Recipes page shows all examples
   - Reference page shows command documentation
   - Platform Guides show Windows/macOS/Linux info
   - Troubleshooting page lists common issues

4. **Links**:
   - Internal links (starting with `/docs/`) work
   - External links to GitHub work
   - Cross-references between pages work

## 🎨 Customization

The home page now uses Lotus Docs shortcodes:

- `{{< blocks/cover >}}` - Hero section
- `{{< blocks/section >}}` - Content sections with different colors
- Bootstrap cards for feature display
- Font Awesome icons for visual elements

You can customize:
- Colors in `hugo.yaml` (primary: #0078D4, secondary: #003d7a)
- Hero text in `content/_index.md`
- Feature cards content
- Platform badges

## 🐛 Common Issues

### Issue: Sections not showing content

**Solution**: Check that `hugo.yaml` has the docs menu configured correctly (already done).

### Issue: Home page not using custom layout

**Solution**: Ensure `content/_index.md` has `layout: "home"` in frontmatter (already done).

### Issue: Hugo modules fail to download

**Solution**:
```bash
hugo mod clean
hugo mod get
```

### Issue: Build fails with "module not found"

**Solution**: Ensure Go 1.21+ is installed and `hugo mod get` has been run.

## 📞 Support

If you encounter issues:

1. Check Hugo version: `hugo version` (must be Extended 0.153.0+)
2. Check Go version: `go version` (must be 1.21+)
3. Review GitHub Actions logs in the Actions tab
4. Check DNS propagation: https://dnschecker.org

## ✨ What's Next

After deployment is successful:

1. **Add screenshots** to documentation pages (placeholders marked with comments)
2. **Create detailed subpages** for OAuth flows (client-credentials.md, etc.)
3. **Add more recipes** (specific API integrations, advanced patterns)
4. **Create platform-specific guides** (windows.md, macos.md, linux.md)
5. **Set up automatic sync** from main repo to docs repo (optional)

---

**Repository Structure:**

```
Main Repository (entra-token-cli)
└── docs/learn/
    ├── content/
    ├── static/
    ├── hugo.yaml
    ├── go.mod
    ├── go.sum
    └── README.md

Documentation Repository (entratool-docs)
├── .github/workflows/hugo.yml
├── content/
├── static/
├── hugo.yaml
├── go.mod
└── go.sum
```

All documentation source files in `docs/learn/` should be pushed to the `entratool-docs` repository for deployment.
