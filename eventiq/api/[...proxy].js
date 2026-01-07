export default async function handler(req, res) {
    const { proxy } = req.query;
  
    // proxy = ['api', 'auth', 'login']
    const path = proxy.join('/');
    console.log('path:', path);
    const targetUrl = `${process.env.BACKEND_BASE}/${path}`;
    
    const response = await fetch(targetUrl, {
      method: req.method,
      headers: {
        ...req.headers,
        host: undefined, 
      },
      body:
        req.method === 'GET' || req.method === 'HEAD'
          ? undefined
          : JSON.stringify(req.body),
    });
  
    const data = await response.text();
  
    res.status(response.status);
    res.send(data);
  }
  