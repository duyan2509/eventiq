console.log('Proxy module loaded at:', new Date().toISOString());

export default async function handler(req, res) {
    console.error('=== PROXY HANDLER CALLED ===');
    console.error('Timestamp:', new Date().toISOString());
    console.error('Method:', req.method);
    console.error('URL:', req.url);
    console.error('Full Query:', JSON.stringify(req.query));
    console.error('Query Keys:', Object.keys(req.query));
    console.error('Headers:', {
      'content-type': req.headers['content-type'],
      'authorization': req.headers['authorization'] ? 'present' : 'missing',
      'user-agent': req.headers['user-agent']
    });
    
    // Also log to console.log
    console.log('=== PROXY HANDLER CALLED ===');
    console.log('Method:', req.method, 'URL:', req.url);
    
    // Handle CORS preflight
    if (req.method === 'OPTIONS') {
      console.log('Handling OPTIONS request');
      res.setHeader('Access-Control-Allow-Origin', '*');
      res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, PATCH, DELETE, OPTIONS');
      res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');
      return res.status(200).end();
    }


    const proxyValue = req.query['...proxy'] || req.query.proxy;
    
    console.error('Proxy value from query:', proxyValue);
    console.error('All query keys:', Object.keys(req.query));
    
    let pathArray;
    if (Array.isArray(proxyValue)) {
      pathArray = proxyValue.map(p => p.split('?')[0]).filter(p => p);
    } else if (proxyValue) {
      const cleanPath = proxyValue.split('?')[0];
      pathArray = cleanPath ? [cleanPath] : [];
    } else {

      const urlPath = req.url?.split('?')[0] || '';
      console.error('No proxy query param, extracting from URL:', urlPath);
      
      const urlMatch = urlPath.match(/^\/api\/(.+)$/);
      if (urlMatch) {
        const fullPath = urlMatch[1];
        pathArray = fullPath.split('/').filter(p => p); // Filter empty strings
        console.error('Extracted path from URL:', pathArray);
      } else if (urlPath === '/api') {
        pathArray = [];
        console.error('Root /api path');
      } else {
        pathArray = [];
        console.error('No path match, using empty array');
      }
    }
    
    const path = pathArray.join('/');
    console.log('Final path:', path);
    
    console.log('Parsed path:', {
      proxyQuery: req.query.proxy,
      pathArray,
      path
    });
  
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
      const url = new URL(apiPath, baseUrl);
      
      if (req.method === 'GET' || req.method === 'HEAD') {
        Object.keys(req.query).forEach(key => {
          // Skip routing parameters
          if (key !== 'proxy' && key !== '...proxy') {
            const value = req.query[key];
            if (Array.isArray(value)) {
              value.forEach(v => url.searchParams.append(key, v));
            } else {
              url.searchParams.append(key, value);
            }
          }
        });
      }
      
      targetUrl = url.toString();
    } catch (error) {
      let url = `${baseUrl}${apiPath}`;
      if (req.method === 'GET' || req.method === 'HEAD') {
        const queryParams = new URLSearchParams();
        Object.keys(req.query).forEach(key => {
          // Skip routing parameters
          if (key !== 'proxy' && key !== '...proxy') {
            const value = req.query[key];
            if (Array.isArray(value)) {
              value.forEach(v => queryParams.append(key, v));
            } else {
              queryParams.append(key, value);
            }
          }
        });
        const queryString = queryParams.toString();
        if (queryString) {
          url += `?${queryString}`;
        }
      }
      targetUrl = url;
    }
    
    const contentType = req.headers['content-type'] || '';
    const isMultipart = contentType.includes('multipart/form-data');
    
    const headers = {};
    
    if (contentType) {
      headers['content-type'] = contentType;
    } else if (req.method !== 'GET' && req.method !== 'HEAD' && !isMultipart) {
      headers['content-type'] = 'application/json';
    }
  
    // Forward authorization
    if (req.headers.authorization) {
      headers['authorization'] = req.headers.authorization;
    }
    
    let body;
    if (req.method === 'GET' || req.method === 'HEAD') {
      body = undefined;
    } else if (isMultipart) {

      if (req.body === undefined || req.body === null) {
        // Body might not be parsed - this is a problem
        console.error('Multipart body is undefined - Vercel may not have parsed it');
        return res.status(400).json({
          error: 'Multipart body not available',
          message: 'Vercel may not parse multipart automatically. Consider using a different approach.'
        });
      }
      
      if (Buffer.isBuffer(req.body)) {
        body = req.body;
      } else if (typeof req.body === 'string') {
        body = Buffer.from(req.body, 'binary');
      } else if (req.body && typeof req.body === 'object') {
        console.warn('Multipart body is object, attempting to reconstruct:', Object.keys(req.body || {}));
        
        try {
          const FormData = (await import('form-data')).default;
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
                formData.append(key, String(value));
              }
            }
          }
          
          body = formData;
          const formDataHeaders = formData.getHeaders();
          headers['content-type'] = formDataHeaders['content-type'];
        } catch (error) {
          console.error('Error reconstructing FormData:', error);
          return res.status(400).json({
            error: 'Failed to process multipart data',
            message: 'Could not reconstruct multipart form data'
          });
        }
      } else {
        console.error('Unexpected body type for multipart:', typeof req.body);
        return res.status(400).json({
          error: 'Invalid multipart body',
          message: `Body type: ${typeof req.body}`
        });
      }
    } else {
      // For JSON requests
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
    
    // Use console.error for better visibility
    console.error('Proxy request details:', {
      method: req.method,
      originalPath: req.url,
      path,
      targetUrl,
      contentType,
      isMultipart,
      bodyType: typeof body,
      isBuffer: Buffer.isBuffer(body),
      hasBody: !!body,
      bodyPreview: typeof body === 'string' ? body.substring(0, 100) : (Buffer.isBuffer(body) ? `Buffer(${body.length} bytes)` : (body ? JSON.stringify(body).substring(0, 100) : 'undefined')),
      headers: Object.keys(headers)
    });
    
    // Also log to console.log
    console.log('Proxy:', req.method, req.url, '->', targetUrl);
  
    try {
      console.log('Fetching:', targetUrl);
      const response = await fetch(targetUrl, {
        method: req.method,
        headers,
        body,
      });
  
      console.log('Response status:', response.status);
      console.log('Response headers:', Object.fromEntries(response.headers.entries()));
  
      const text = await response.text();
      console.log('Response body preview:', text.substring(0, 200));
      
      let data;
      try {
        data = JSON.parse(text);
      } catch {
        data = text;
      }
  
      // Forward CORS headers
      const corsHeaders = {
        'Access-Control-Allow-Origin': response.headers.get('Access-Control-Allow-Origin') || '*',
        'Access-Control-Allow-Methods': response.headers.get('Access-Control-Allow-Methods') || 'GET, POST, PUT, PATCH, DELETE',
        'Access-Control-Allow-Headers': response.headers.get('Access-Control-Allow-Headers') || 'Content-Type, Authorization',
      };
      
      Object.entries(corsHeaders).forEach(([key, value]) => {
        if (value) res.setHeader(key, value);
      });
      
      res.status(response.status);
      
      const responseContentType = response.headers.get('content-type');
      if (responseContentType) {
        res.setHeader('content-type', responseContentType);
      }
      
      res.json(data);
    } catch (error) {
      console.error('Proxy fetch error:', error);
      console.error('Error stack:', error.stack);
      res.status(502).json({ 
        error: 'Proxy error',
        message: error.message,
        targetUrl: targetUrl
      });
    }
  }
  