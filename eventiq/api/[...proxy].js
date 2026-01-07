export default async function handler(req, res) {
    const { proxy = [] } = req.query;
    const path = proxy.join('/');
  
    const targetUrl = `${process.env.BACKEND_BASE}/${path}`;
    console.log('Proxy →', targetUrl);
  
    const headers = {
      'content-type': req.headers['content-type'],
    };
  
    if (req.headers.authorization) {
      headers['authorization'] = req.headers.authorization;
    }
  
    const response = await fetch(targetUrl, {
      method: req.method,
      headers,
      body:
        req.method === 'GET' || req.method === 'HEAD'
          ? undefined
          : JSON.stringify(req.body),
    });
  
    const text = await response.text();
  
    res.status(response.status);
    res.send(text);
  }
  