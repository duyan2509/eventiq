import React from 'react';
import SeatMapDesigner from '../components/SeatMapDesigner';

const SeatMapDesignerPage = () => {
    return (
        <div style={{ 
            height: '100vh', 
            width: '100vw', 
            position: 'fixed', 
            top: 0, 
            left: 0, 
            backgroundColor: '#f9fafb' 
        }}>
            <SeatMapDesigner />
        </div>
    );
};

export default SeatMapDesignerPage;