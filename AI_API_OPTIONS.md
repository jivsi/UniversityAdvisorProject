# Free AI API Options for University Finder

## Overview
The chatbot feature in University Finder requires an AI API key. Here are your options, including free alternatives:

## Free AI API Options

### 1. **Hugging Face Inference API** (Recommended for Free Tier)
- **Website**: https://huggingface.co/inference-api
- **Free Tier**: Yes, with rate limits
- **How to Get**: 
  1. Sign up at https://huggingface.co
  2. Go to Settings → Access Tokens
  3. Create a new token
- **Rate Limits**: Free tier has limited requests per month
- **Models Available**: Many open-source models including Llama, Mistral, etc.
- **Note**: You'll need to modify the `OpenAiService.cs` to use Hugging Face API instead of OpenAI

### 2. **Cohere API**
- **Website**: https://cohere.com
- **Free Tier**: Yes, with generous limits
- **How to Get**:
  1. Sign up at https://cohere.com
  2. Get your API key from the dashboard
  3. Free tier includes a good number of requests
- **Rate Limits**: Generous free tier
- **Models**: Cohere's own models (Command, Command-Light)

### 3. **Google Gemini API**
- **Website**: https://ai.google.dev
- **Free Tier**: Yes, with limits
- **How to Get**:
  1. Sign up at https://makersuite.google.com
  2. Get API key from Google Cloud Console
  3. Free tier available
- **Rate Limits**: Limited but reasonable for testing
- **Models**: Gemini Pro, Gemini Pro Vision

### 4. **OpenAI API** (Paid, but has credits)
- **Website**: https://platform.openai.com
- **Free Tier**: No longer available, but new accounts get $5 free credits
- **How to Get**:
  1. Sign up at https://platform.openai.com
  2. Add payment method (but you get $5 free credits)
  3. Get API key from API Keys section
- **Cost**: Pay-as-you-go after free credits
- **Models**: GPT-3.5-turbo (cheapest), GPT-4 (more expensive)

## Paid Options (No Free Tier)

### 5. **Anthropic Claude API**
- **Website**: https://www.anthropic.com
- **Free Tier**: No
- **Cost**: Pay-as-you-go
- **Models**: Claude 3 (various sizes)

## Recommendation

**For Free Use**: 
- **Hugging Face** or **Cohere** are your best bets
- Both have reasonable free tiers
- Hugging Face has more model options
- Cohere is easier to integrate (similar to OpenAI)

**For Best Quality**:
- **OpenAI GPT-3.5-turbo** is very affordable ($0.0015 per 1K tokens)
- New accounts get $5 free credits (enough for thousands of requests)
- Best balance of cost and quality

## Implementation Notes

The current implementation uses OpenAI API. To switch to a different provider:

1. **For Hugging Face**: You'll need to modify `OpenAiService.cs` to use Hugging Face endpoints
2. **For Cohere**: Similar structure to OpenAI, easier to adapt
3. **For Google Gemini**: Different API structure, requires more changes

## Current Configuration

The API key is configured in `appsettings.json`:
```json
"OpenAI": {
  "ApiKey": "YOUR_API_KEY_HERE"
}
```

You can also use User Secrets for development:
```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
```

## Testing Without API Key

The chatbot will show a helpful message if no API key is configured, directing users to set one up.
