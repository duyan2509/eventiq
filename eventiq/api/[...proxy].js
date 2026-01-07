export default async function handler(req, res) {
    const { proxy = [] } = req.query;
    const path = proxy.join('/');
  
    const backendBase = process.env.VITE_BACKEND_BASE;
    
    if (!backendBase) {
      console.error('VITE_BACKEND_BASE is not set');
      return res.status(500).json({ 
        error: 'Backend URL not configured',
        message: 'VITE_BACKEND_BASE environment variable is missing' 
      });
    }
    
    const targetUrl = `${backendBase}/api/${path}`;
    console.log('Proxy →', targetUrl);
  
    const headers = {
      'content-type': req.headers['content-type'] || 'application/json',
    };
  
    if (req.headers.authorization) {
      headers['authorization'] = req.headers.authorization;
    }
  
    try {
      const response = await fetch(targetUrl, {
        method: req.method,
        headers,
        body:
          req.method === 'GET' || req.method === 'HEAD'
            ? undefined
            : JSON.stringify(req.body),
      });
  
      const text = await response.text();
      
      // Try to parse as JSON, if fails return as text
      let data;
      try {
        data = JSON.parse(text);
      } catch {
        data = text;
      }
  
      res.status(response.status);
      
      // Set content-type based on response
      const contentType = response.headers.get('content-type');
      if (contentType) {
        res.setHeader('content-type', contentType);
      }
      
      res.json(data);
    } catch (error) {
      console.error('Proxy error:', error);
      res.status(502).json({ 
        error: 'Proxy error',
        message: error.message 
      });
    }
  }
  