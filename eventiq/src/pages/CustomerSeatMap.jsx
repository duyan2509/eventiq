import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Typography,
  Spin,
  Empty,
  Button,
  Space,
  Alert,
  FloatButton,
  Modal,
  Descriptions,
  Tag,
} from 'antd';
import { DollarOutlined, ArrowLeftOutlined, ShoppingCartOutlined, CloseOutlined } from '@ant-design/icons';
import { SeatsioSeatingChart } from '@seatsio/seatsio-react';
import { customerAPI } from '../services/api';

const { Title, Text } = Typography;

const CustomerSeatMap = () => {
  const { eventId, eventItemId } = useParams();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [seatMap, setSeatMap] = useState(null);
  const [selectedSeats, setSelectedSeats] = useState([]);
  const [error, setError] = useState(null);
  const [chartRendering, setChartRendering] = useState(false);
  const [clickedSeat, setClickedSeat] = useState(null);
  const [seatModalVisible, setSeatModalVisible] = useState(false);
  const [cartVisible, setCartVisible] = useState(false);
  const [bookedSeats, setBookedSeats] = useState([]);
  const [holdSeats, setHoldSeats] = useState([]);
  // Seats that are already purchased (paid) - shown as booked/red

  useEffect(() => {
    if (eventId && eventItemId) {
      fetchSeatMap();
    }
  }, [eventId, eventItemId]);

  // Timeout for chart rendering - auto-hide loading after 30 seconds
  useEffect(() => {
    if (chartRendering) {
      const timeout = setTimeout(() => {
        console.warn('Chart rendering timeout - hiding loading spinner after 30 seconds');
        setChartRendering(false);
      }, 30000); // 30 seconds timeout

      return () => clearTimeout(timeout);
    }
  }, [chartRendering]);

  const fetchSeatMap = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await customerAPI.getEventItemSeatMap(eventId, eventItemId);
      console.log('Seat map data received:', data);
      console.log('ChartKey:', data.chartKey);
      console.log('Public Key:', import.meta.env.VITE_SEAT_IO_PUBLIC_KEY);

      // Add mock data for testing: 2 random seats with different statuses
      // 1 seat with 'paid' status (already purchased) - shown as booked/red
      // 1 seat with 'hold' status (currently being paid) - shown as hold/yellow
      // This modifies the status of existing seats for frontend testing
      addMockSeatsForTesting(data);

    } catch (error) {
      console.error('Error fetching seat map:', error);
      setError(error.response?.data?.message || error.message || 'Failed to load seat map');
    } finally {
      setLoading(false);
    }
  };

  // Helper function to add mock seats for testing frontend
  // This simulates seats that are already purchased or being paid
  // Changes status of 2 random free seats to 'paid' and 'hold' for testing
  const addMockSeatsForTesting = (seatMapData) => {
    if (!seatMapData || !seatMapData.seats || seatMapData.seats.length === 0) {
      console.warn('No seats found in seat map data');
      return [];
    }

    // Get random seats from existing seats that are free
    const availableSeats = seatMapData.seats.filter(s => s.status === 'free');
    if (availableSeats.length < 2) {
      console.warn('Not enough free seats to create mock data');
      return [];
    }

    // Randomly select 2 different seats
    const shuffled = [...availableSeats].sort(() => 0.5 - Math.random());
    const selectedSeats = shuffled.slice(0, 2);

    // Update status of selected seats to simulate different states
    // This modifies the original seats array
    selectedSeats[0].status = 'reservedByToken'; // Already purchased - will show as booked/red
    selectedSeats[1].status = 'booked'; // Currently being paid - will show as hold/yellow

    console.log('Mock seats for testing (status changed):', {
      paid: { seatKey: selectedSeats[0].seatKey, status: selectedSeats[0].status },
      hold: { seatKey: selectedSeats[1].seatKey, status: selectedSeats[1].status }
    });
    setSeatMap(seatMapData);
    setBookedSeats(seatMapData.seats
      .filter((s) => s.status === 'booked')
      .map((s) => s.label));

    // Seats that are currently being paid (hold) - shown as hold/yellow
    // These are seats in the checkout process, reserved temporarily
    setHoldSeats(seatMapData.seats
      .filter((s) => s.status === 'reservedByToken')
      .map((s) => s.label))
    // Return empty array since we modified the original seats
    return [];
  };

  // Helper function to map Seats.io object to our seat format
  const mapSeatObject = (obj) => {
    const seatId = obj.id || obj.objectId; // Support both 'id' and 'objectId'
    const seat = seatMap?.seats.find((s) => s.seatKey === seatId);

    // Get price from multiple sources
    const seatPrice = obj.price
      || obj.pricing?.price
      || obj.category?.pricing?.price
      || seat?.price
      || 0;

    return {
      seatKey: seatId,
      label: obj.label || obj.labels?.own || seatId,
      price: Number(seatPrice), // Ensure it's a number
      categoryKey: obj.categoryKey || obj.category?.key,
      status: seat?.status || 'available',
      ...seat,
    };
  };

  const handleObjectSelected = (selectedObject) => {
    console.log('=== onObjectSelected called ===');
    console.log('Raw selectedObject:', selectedObject);

    // Handle both single object and array cases
    const selectedArray = Array.isArray(selectedObject)
      ? selectedObject
      : selectedObject
        ? [selectedObject]
        : [];

    console.log('Selected array:', selectedArray);

    // Map new seats from Seats.io
    const newSeats = selectedArray.map(mapSeatObject);
    console.log('New seats mapped:', newSeats);

    // Merge with existing selected seats: add new ones, keep existing ones
    setSelectedSeats(prevSeats => {
      console.log('Previous selectedSeats:', prevSeats);

      // Create a map of existing seats by seatKey for quick lookup
      const existingSeatsMap = new Map(prevSeats.map(s => [s.seatKey, s]));

      // Add new seats (will overwrite if already exists, which is fine)
      newSeats.forEach(seat => {
        existingSeatsMap.set(seat.seatKey, seat);
      });

      // Convert back to array
      const updatedSeats = Array.from(existingSeatsMap.values());

      console.log('=== Merged selected seats ===', {
        previous: prevSeats.length,
        new: newSeats.length,
        final: updatedSeats.length,
        seats: updatedSeats.map(s => ({ key: s.seatKey, label: s.label, price: s.price }))
      });

      return updatedSeats;
    });
  };

  const handleObjectDeselected = (deselectedObject) => {
    console.log('=== onObjectDeselected called ===');
    console.log('Raw deselectedObject:', deselectedObject);

    // Handle both single object and array cases
    const deselectedArray = Array.isArray(deselectedObject)
      ? deselectedObject
      : deselectedObject
        ? [deselectedObject]
        : [];

    // Get seat IDs to remove
    const seatIdsToRemove = deselectedArray.map(obj => obj.id || obj.objectId);

    console.log('Seat IDs to remove:', seatIdsToRemove);
    console.log('Current selectedSeats before removal:', selectedSeats);

    // Remove deselected seats
    setSelectedSeats(prevSeats => {
      const updatedSeats = prevSeats.filter(s => !seatIdsToRemove.includes(s.seatKey));

      console.log('=== Updated selected seats after removal ===', {
        previous: prevSeats.length,
        removed: seatIdsToRemove.length,
        final: updatedSeats.length,
        seats: updatedSeats
      });

      return updatedSeats;
    });
  };

  const handleObjectClick = (clickedObject) => {
    console.log('Seat clicked:', clickedObject);
    // Handle both single object and array cases (in case multiple objects are clicked)
    const clickedObj = Array.isArray(clickedObject)
      ? clickedObject[0]
      : clickedObject;

    // Seats.io uses 'id' instead of 'objectId'
    const seatId = clickedObj?.id || clickedObj?.objectId;

    if (!clickedObj || !seatId) {
      console.warn('Invalid clicked object:', clickedObject);
      return;
    }

    // Find seat details from seatMap
    const seat = seatMap?.seats.find((s) => s.seatKey === seatId);

    // Get price from object or from seat data
    const seatPrice = clickedObj.price || clickedObj.pricing?.price || seat?.price || 0;

    const seatInfo = {
      seatKey: seatId,
      label: clickedObj.label || clickedObj.labels?.own || seatId,
      price: seatPrice,
      categoryKey: clickedObj.categoryKey || clickedObj.category?.key,
      status: seat?.status || 'available',
      ...seat,
    };
    setClickedSeat(seatInfo);
    setSeatModalVisible(true);
  };

  const handleProceedToCheckout = () => {
    if (selectedSeats.length === 0) {
      return;
    }
    // Save selected seats to localStorage
    localStorage.setItem('selectedSeats', JSON.stringify(selectedSeats));
    localStorage.setItem('eventId', eventId);
    localStorage.setItem('eventItemId', eventItemId);
    navigate('/payment');
  };

  const getTotalPrice = () => {
    return selectedSeats.reduce((sum, seat) => sum + (seat.price || 0), 0);
  };

  const formatPrice = (price) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(price);
  };

  const publicKey = import.meta.env.VITE_SEAT_IO_PUBLIC_KEY;

  if (loading) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center'
      }}>
        <Spin size="large" tip="Loading seat map..." />
      </div>
    );
  }

  if (error) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        padding: '50px'
      }}>
        <Alert
          message="Error"
          description={error}
          type="error"
          showIcon
          action={
            <Button size="small" onClick={() => navigate(-1)}>
              Go Back
            </Button>
          }
        />
      </div>
    );
  }

  if (!seatMap) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center'
      }}>
        <Empty description="Seat map not found" />
        <Button type="primary" onClick={() => navigate(-1)} style={{ marginTop: 16 }}>
          Go Back
        </Button>
      </div>
    );
  }

  // Check for required configuration
  if (!publicKey) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        padding: '50px'
      }}>
        <Alert
          message="Configuration Error"
          description="Seats.io public key is not configured. Please check VITE_SEAT_IO_PUBLIC_KEY in .env file."
          type="error"
          showIcon
        />
        <Button type="primary" onClick={() => navigate(-1)} style={{ marginTop: 16 }}>
          Go Back
        </Button>
      </div>
    );
  }

  // Check if we have either eventKey or chartKey
  const hasKey = (seatMap.eventKey && seatMap.eventKey.trim() !== '') ||
    (seatMap.chartKey && seatMap.chartKey.trim() !== '');

  if (!hasKey) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        padding: '50px'
      }}>
        <Alert
          message="Seat Map Not Available"
          description="This session does not have a seat map configured yet."
          type="warning"
          showIcon
        />
        <Button type="primary" onClick={() => navigate(-1)} style={{ marginTop: 16 }}>
          Go Back
        </Button>
      </div>
    );
  }


  // Prepare selected objects (for user's current selection)
  // This is different from hold - these are seats the current user is selecting
  const selectedObjects = selectedSeats.map(s => s.seatKey);

  // Use EventKey if available (preferred), otherwise fallback to ChartKey
  const eventKey = seatMap.eventKey || seatMap.chartKey;

  // If we have eventKey from Seats.io, use it directly (no need to check GUID)
  // Only check GUID format if we're using chartKey as fallback
  const usingEventKey = !!seatMap.eventKey;
  const isGuidFormat = !usingEventKey && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(eventKey);

  let venueDefinition = null;
  if (seatMap.venueDefinition) {
    try {
      venueDefinition = typeof seatMap.venueDefinition === 'string'
        ? JSON.parse(seatMap.venueDefinition)
        : seatMap.venueDefinition;
    } catch (e) {
      console.warn('Failed to parse venueDefinition:', e);
    }
  }

  // If using chartKey (not eventKey) and it's GUID format but no venueDefinition, cannot render
  if (!usingEventKey && isGuidFormat && !venueDefinition) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        padding: '50px'
      }}>
        <Alert
          message="Seat Map Not Configured"
          description={
            <div>
              <p>The seat map for this session has not been properly configured yet.</p>
              <p>The chart key appears to be a temporary identifier instead of a Seats.io chart key.</p>
              <p style={{ marginTop: 8 }}>Please contact the event organizer to complete the seat map setup.</p>
            </div>
          }
          type="warning"
          showIcon
        />
        <Button type="primary" onClick={() => navigate(-1)} style={{ marginTop: 16 }}>
          Go Back
        </Button>
      </div>
    );
  }

  // Build pricing configuration from ticketClasses
  // Pricing is set at category level - each category (ticketClass.Name) gets its price
  const pricingConfig = seatMap.ticketClasses && seatMap.ticketClasses.length > 0
    ? {
      priceFormatter: (price) => '₫' + price,
      prices: seatMap.ticketClasses.map(tc => ({
        category: tc.name || tc.Name, // Category key is ticketClass.Name
        price: tc.price || tc.Price || 0
      }))
    }
    : null;

  console.log('Rendering Seats.io chart with:', {
    workspaceKey: publicKey,
    eventKey: seatMap.eventKey,
    chartKey: seatMap.chartKey,
    usingEventKey: usingEventKey,
    finalEventKey: eventKey,
    isGuid: isGuidFormat,
    hasVenueDefinition: !!venueDefinition,
    bookedSeats: bookedSeats,
    selectedObjects: selectedObjects,
    bookedSeatsCount: bookedSeats.length,
    totalSeats: seatMap.seats.length,
    pricingConfig: pricingConfig,
  });

  return (
    <div style={{
      height: '100vh',
      width: '100vw',
      position: 'fixed',
      top: 0,
      left: 0,
      backgroundColor: '#f9fafb',
      display: 'flex',
      flexDirection: 'column'
    }}>
      {/* Header */}
      <div style={{
        padding: '16px 24px',
        backgroundColor: '#fff',
        borderBottom: '1px solid #d9d9d9',
        zIndex: 1000,
        flexShrink: 0
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
          <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
            <Button
              icon={<ArrowLeftOutlined />}
              onClick={() => navigate(-1)}
            >
              Back
            </Button>
            <Title level={4} style={{ margin: 0 }}>
              {seatMap.eventItemName}
            </Title>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>

            {selectedSeats.length > 0 && (
              <Text strong style={{ color: '#f5222d', fontSize: 16 }}>
                <DollarOutlined /> {formatPrice(getTotalPrice())}
              </Text>
            )}
          </div>
        </div>
      </div>

      {/* Seats.io Chart Container - Full Screen */}
      <div style={{
        flex: 1,
        width: '100%',
        position: 'relative',
        overflow: 'hidden',
        minHeight: 0
      }}>
        {chartRendering && (
          <div style={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            zIndex: 10
          }}>
            <Spin size="large" tip="Rendering seat map..." />
          </div>
        )}
        {eventKey && (
          <SeatsioSeatingChart
            key={`${eventKey}-${usingEventKey}`} // Force re-render when key changes
            workspaceKey={publicKey}
            {...(venueDefinition && !usingEventKey
              ? { event: venueDefinition }
              : { event: eventKey })}
            bookedObjects={[...bookedSeats, ...holdSeats]}
            selectedObjects={selectedObjects}
            {...(seatMap.maxPerUser > 0 || seatMap.MaxPerUser > 0
              ? {
                maxSelectedObjects: (seatMap.maxPerUser || seatMap.MaxPerUser || 0) // Total max seats per user for this event item
              }
              : {})}
            {...(pricingConfig ? { pricing: pricingConfig } : {})}
            region="OC"
            language="en"
            onRenderStarted={(chart) => {
              console.log('Seats.io chart render started', chart);
              console.log(chart.selectedObjects);

              setChartRendering(true);
              setError(null); // Clear any previous errors

            }}
            onObjectSelected={handleObjectSelected}
            onObjectDeselected={handleObjectDeselected}
            onObjectClick={handleObjectClick}
            onChartRendered={(chart) => {
              console.log('Seats.io chart rendered successfully', chart);
              setChartRendering(false);
              setError(null); // Clear any errors on success
            }}
            onChartRenderingFailed={(error) => {
              console.error('Seats.io chart rendering failed:', error);
              console.error('Error details:', {
                error,
                message: error?.message,
                config: error?.config,
                eventKey: seatMap.eventKey,
                chartKey: seatMap.chartKey,
                workspaceKey: publicKey,
                usingEventKey: usingEventKey
              });
              setChartRendering(false);
              // Try to extract error message from various possible locations
              const errorMessage = error?.message
                || error?.error?.message
                || error?.config?.error?.message
                || (typeof error === 'string' ? error : 'Failed to render seat map. Please check if the seat map is properly configured.');
              setError(errorMessage);
            }}
            showLegend={true}


          />
        )}
      </div>

      {/* Floating Checkout Button and Cart */}
      {selectedSeats.length > 0 && (
        <>
          {/* Cart Summary - Visible when cartVisible is true */}
          {cartVisible && (
            <div
              style={{
                position: 'fixed',
                bottom: 100,
                right: 24,
                background: '#fff',
                padding: 16,
                borderRadius: 8,
                boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
                minWidth: 280,
                maxWidth: 320,
                zIndex: 1000,
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                <Title level={5} style={{ margin: 0 }}>
                  Order Summary
                </Title>
                <Button
                  type="text"
                  icon={<CloseOutlined />}
                  onClick={() => setCartVisible(false)}
                  style={{ padding: 0, width: 24, height: 24 }}
                />
              </div>
              <div style={{ marginBottom: 8 }}>
                <Text>Selected Seats: </Text>
                <Text strong>{selectedSeats.length}</Text>
              </div>
              <div style={{ marginBottom: 8, maxHeight: 150, overflowY: 'auto', border: '1px solid #f0f0f0', padding: 8, borderRadius: 4, backgroundColor: '#fafafa' }}>
                {selectedSeats.length > 0 ? (
                  selectedSeats.map((seat, index) => (
                    <div key={seat.seatKey || index} style={{
                      marginBottom: 4,
                      fontSize: 12,
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      padding: '4px 8px',
                      backgroundColor: '#fff',
                      borderRadius: 4,
                      border: '1px solid #e8e8e8'
                    }}>
                      <Text strong style={{ fontSize: 13 }}>{seat.label || seat.seatKey}</Text>
                      <Text style={{ marginLeft: 8, color: '#f5222d', fontWeight: 'bold', fontSize: 13 }}>
                        {formatPrice(seat.price || 0)}
                      </Text>
                    </div>
                  ))
                ) : (
                  <Text type="secondary" style={{ fontSize: 12, fontStyle: 'italic' }}>No seats selected</Text>
                )}
              </div>
              <div style={{ marginBottom: 16, borderTop: '1px solid #d9d9d9', paddingTop: 8 }}>
                <Text strong>
                  <DollarOutlined /> Total:{' '}
                </Text>
                <Text strong style={{ color: '#f5222d', fontSize: 18 }}>
                  {formatPrice(getTotalPrice())}
                </Text>
              </div>
              <Button
                type="primary"
                size="large"
                block
                onClick={handleProceedToCheckout}
              >
                Proceed to Checkout
              </Button>
            </div>
          )}

          {/* Floating Action Button - Toggle cart visibility */}
          <FloatButton
            type="primary"
            style={{ right: 24, bottom: 24 }}
            icon={<ShoppingCartOutlined />}
            tooltip={cartVisible ? 'Hide cart' : `Show cart (${selectedSeats.length} seat(s))`}
            badge={{ count: selectedSeats.length, overflowCount: 99 }}
            onClick={() => setCartVisible(!cartVisible)}
          />
        </>
      )}

      {/* Seat Information Modal */}
      <Modal
        title="Seat Information"
        open={seatModalVisible}
        onCancel={() => setSeatModalVisible(false)}
        footer={[
          <Button key="close" onClick={() => setSeatModalVisible(false)}>
            Close
          </Button>,
          clickedSeat?.status === 'available' && (
            <Button
              key="select"
              type="primary"
              onClick={() => {
                // Add to selected seats if not already selected
                const isAlreadySelected = selectedSeats.some(
                  (s) => s.seatKey === clickedSeat.seatKey
                );
                if (!isAlreadySelected) {
                  setSelectedSeats([...selectedSeats, clickedSeat]);
                }
                setSeatModalVisible(false);
              }}
            >
              Select This Seat
            </Button>
          ),
        ].filter(Boolean)}
        width={500}
      >
        {clickedSeat && (
          <Descriptions column={1} bordered>
            <Descriptions.Item label="Seat Label">
              <Text strong>{clickedSeat.label || clickedSeat.seatKey}</Text>
            </Descriptions.Item>
            <Descriptions.Item label="Price">
              <Text strong style={{ color: '#f5222d', fontSize: 18 }}>
                <DollarOutlined /> {formatPrice(clickedSeat.price || 0)}
              </Text>
            </Descriptions.Item>
            <Descriptions.Item label="Status">
              <Tag
                color={
                  clickedSeat.status === 'available'
                    ? 'green'
                    : clickedSeat.status === 'paid'
                      ? 'red'
                      : clickedSeat.status === 'hold'
                        ? 'orange'
                        : 'default'
                }
              >
                {clickedSeat.status === 'available'
                  ? 'Available'
                  : clickedSeat.status === 'paid'
                    ? 'Sold'
                    : clickedSeat.status === 'hold'
                      ? 'On Hold'
                      : clickedSeat.status || 'Unknown'}
              </Tag>
            </Descriptions.Item>
            {clickedSeat.categoryKey && (
              <Descriptions.Item label="Category">
                <Tag>{clickedSeat.categoryKey}</Tag>
              </Descriptions.Item>
            )}
            {clickedSeat.seatKey && (
              <Descriptions.Item label="Seat Key">
                <Text code>{clickedSeat.seatKey}</Text>
              </Descriptions.Item>
            )}
          </Descriptions>
        )}
      </Modal>
    </div>
  );
};

export default CustomerSeatMap;
