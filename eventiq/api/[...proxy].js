export default async function handler(req, res) {
    // Handle CORS preflight
    if (req.method === 'OPTIONS') {
      res.setHeader('Access-Control-Allow-Origin', '*');
      res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, PATCH, DELETE, OPTIONS');
      res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');
      return res.status(200).end();
    }

    // Log incoming request for debugging
    console.log('=== Proxy Handler Called ===');
    console.log('Method:', req.method);
    console.log('URL:', req.url);
    console.log('Query:', req.query);
    console.log('Headers:', {
      'content-type': req.headers['content-type'],
      'authorization': req.headers['authorization'] ? 'present' : 'missing'
    });

    const { proxy } = req.query;
    
    let pathArray;
    if (Array.isArray(proxy)) {
      pathArray = proxy;
    } else if (proxy) {
      pathArray = [proxy];
    } else {
      // Fallback: extract from URL
      const urlMatch = req.url?.match(/^\/api\/(.+)$/);
      pathArray = urlMatch ? urlMatch[1].split('/') : [];
    }
    
    const path = pathArray.join('/');
    
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
      targetUrl = new URL(apiPath, baseUrl).toString();
    } catch (error) {
      targetUrl = `${baseUrl}${apiPath}`;
    }
    
    const contentType = req.headers['content-type'] || '';
    const isMultipart = contentType.includes('multipart/form-data');
    
    // Prepare headers - forward all important headers
    const headers = {};
    
    // Forward content-type (important for multipart with boundary)
    if (contentType) {
      headers['content-type'] = contentType;
    } else if (req.method !== 'GET' && req.method !== 'HEAD' && !isMultipart) {
      headers['content-type'] = 'application/json';
    }
  
    // Forward authorization
    if (req.headers.authorization) {
      headers['authorization'] = req.headers.authorization;
    }
    
    // Prepare body
    let body;
    if (req.method === 'GET' || req.method === 'HEAD') {
      body = undefined;
    } else if (isMultipart) {
      // For multipart, Vercel may not parse it automatically
      // Try to get raw body - Vercel stores it in req.body but may be in different formats
      
      // Check if we can access the raw request stream
      // Vercel serverless functions may provide body as Buffer, string, or undefined
      if (req.body === undefined || req.body === null) {
        // Body might not be parsed - this is a problem
        console.error('Multipart body is undefined - Vercel may not have parsed it');
        return res.status(400).json({
          error: 'Multipart body not available',
          message: 'Vercel may not parse multipart automatically. Consider using a different approach.'
        });
      }
      
      // Try different body formats
      if (Buffer.isBuffer(req.body)) {
        // Raw multipart data as Buffer - use directly
        body = req.body;
      } else if (typeof req.body === 'string') {
        // String representation - convert to Buffer
        body = Buffer.from(req.body, 'binary');
      } else if (req.body && typeof req.body === 'object') {
        // Vercel may have parsed multipart into an object (unlikely but possible)
        console.warn('Multipart body is object, attempting to reconstruct:', Object.keys(req.body || {}));
        
        // Try to reconstruct FormData if we have form-data package available
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
          // Fallback: return error
          return res.status(400).json({
            error: 'Failed to process multipart data',
            message: 'Could not reconstruct multipart form data'
          });
        }
      } else {
        // Body is undefined or unexpected type
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
    
    console.log('Proxy request details:', {
      method: req.method,
      originalPath: req.url,
      path,
      targetUrl,
      contentType,
      isMultipart,
      bodyType: typeof body,
      isBuffer: Buffer.isBuffer(body),
      hasBody: !!body,
      bodyPreview: typeof body === 'string' ? body.substring(0, 100) : (Buffer.isBuffer(body) ? `Buffer(${body.length} bytes)` : JSON.stringify(body).substring(0, 100)),
      headers: headers
    });
  
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
  