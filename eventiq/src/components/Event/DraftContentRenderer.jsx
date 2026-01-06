import React from 'react';
import { Editor, EditorState } from 'draft-js';
import { convertFromRaw } from 'draft-js';
import 'draft-js/dist/Draft.css';

const styleMap = {
  'BOLD': { fontWeight: 'bold' },
  'ITALIC': { fontStyle: 'italic' },
  'UNDERLINE': { textDecoration: 'underline' },
};

const DraftContentRenderer = ({ content }) => {
  if (!content) return null;

  try {
    const rawContent = typeof content === 'string' ? JSON.parse(content) : content;
    const contentState = convertFromRaw(rawContent);
    const editorState = EditorState.createWithContent(contentState);

    return (
      <div style={{ 
        border: '1px solid #d9d9d9', 
        borderRadius: '4px', 
        padding: '12px',
        minHeight: '100px'
      }}>
        <Editor
          editorState={editorState}
          readOnly={true}
          customStyleMap={styleMap}
        />
      </div>
    );
  } catch (error) {
    return <div>{typeof content === 'string' ? content : JSON.stringify(content)}</div>;
  }
};

export default DraftContentRenderer;

