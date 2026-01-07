import FormData from 'form-data';

export default async function handler(req, res) {
    // Handle CORS preflight
    if (req.method === 'OPTIONS') {
      res.setHeader('Access-Control-Allow-Origin', '*');
      res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, PATCH, DELETE, OPTIONS');
      res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');
      return res.status(200).end();
    }

    const { proxy } = req.query;
    
    let pathArray;
    if (Array.isArray(proxy)) {
      pathArray = proxy;
    } else if (proxy) {
      pathArray = [proxy];
    } else {
      const urlMatch = req.url?.match(/^\/api\/(.+)$/);
      pathArray = urlMatch ? urlMatch[1].split('/') : [];
    }
    
    const path = pathArray.join('/');
  
    const backendBase = process.env.VITE_BACKEND_BASE;
    
    if (!backendBase) {
      console.error('VITE_BACKEND_BASE is not set');
      return res.status(500).json({ 
        error: 'Backend URL not configured',
        message: 'VITE_BACKEND_BASE environment variable is missing' 
      });
    }
    
    const baseUrl = backendBase.endsWith('/') ? backendBase.slice(0, -1) : backendBase;
    const apiPath = path ? `/api/${path}` : '/api';
    
    let targetUrl;
    try {
      targetUrl = new URL(apiPath, baseUrl).toString();
    } catch (error) {
      targetUrl = `${baseUrl}${apiPath}`;
    }
    
    const contentType = req.headers['content-type'] || '';
    const isMultipart = contentType.includes('multipart/form-data');
    
    const headers = {};
    
    if (contentType) {
      headers['content-type'] = contentType;
    } else if (req.method !== 'GET' && req.method !== 'HEAD' && !isMultipart) {
      headers['content-type'] = 'application/json';
    }
  
    if (req.headers.authorization) {
      headers['authorization'] = req.headers.authorization;
    }
    
    // Prepare body
    let body;
    if (req.method === 'GET' || req.method === 'HEAD') {
      body = undefined;
    } else if (isMultipart) {
      
      if (Buffer.isBuffer(req.body)) {
        body = req.body;
      } else if (typeof req.body === 'string') {
        body = Buffer.from(req.body, 'binary');
      } else if (req.body && typeof req.body === 'object') {
        try {
          const formData = new FormData();
          
          for (const [key, value] of Object.entries(req.body)) {
            if (value !== null && value !== undefined) {
              if (Buffer.isBuffer(value) || (typeof value === 'object' && value.data)) {
                const fileData = Buffer.isBuffer(value) ? value : Buffer.from(value.data);
                const filename = value.filename || value.name || key;
                const fileContentType = value.contentType || value.type || 'application/octet-stream';
                
                formData.append(key, fileData, {
                  filename: filename,
                  contentType: fileContentType
                });
              } else {
                // Regular field
                formData.append(key, String(value));
              }
            }
          }
          
          body = formData;
          const formDataHeaders = formData.getHeaders();
          headers['content-type'] = formDataHeaders['content-type'];
        } catch (error) {
          console.error('Error reconstructing FormData:', error);
          body = req.body;
        }
      } else {
        body = req.body;
      }
    } else {
      if (req.body) {
        if (typeof req.body === 'string') {
          body = req.body;
        } else {
          body = JSON.stringify(req.body);
        }
      } else {
        body = undefined;
      }
    }
    
    console.log('Proxy request:', {
      method: req.method,
      originalPath: req.url,
      path,
      targetUrl,
      hasBody: !!body,
      bodyLength: body?.length,
      headers
    });
  
    try {
      const response = await fetch(targetUrl, {
        method: req.method,
        headers,
        body,
      });
  
      const text = await response.text();
      
      let data;
      try {
        data = JSON.parse(text);
      } catch {
        data = text;
      }
  
      const corsHeaders = {
        'Access-Control-Allow-Origin': response.headers.get('Access-Control-Allow-Origin') || '*',
        'Access-Control-Allow-Methods': response.headers.get('Access-Control-Allow-Methods') || 'GET, POST, PUT, PATCH, DELETE',
        'Access-Control-Allow-Headers': response.headers.get('Access-Control-Allow-Headers') || 'Content-Type, Authorization',
      };
      
      Object.entries(corsHeaders).forEach(([key, value]) => {
        if (value) res.setHeader(key, value);
      });
      
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
  